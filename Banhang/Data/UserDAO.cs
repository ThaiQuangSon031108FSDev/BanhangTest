using System;
using System.Collections.Generic;
using Banhang.Common;
using Banhang.Helpers;
using Banhang.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Banhang.Data
{
    public class UserDAO
    {
        private readonly string _connStr;
        private readonly ILogger<UserDAO> _logger;

        public UserDAO(string connStr, ILogger<UserDAO> logger)
        {
            _connStr = connStr;
            _logger = logger;
        }

        public User? CheckLogin(string username, string password, out bool passwordUpgraded)
        {
            passwordUpgraded = false;

            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"
                    SELECT u.UserID, u.Username, u.FullName, u.Email, u.Phone, u.RoleID,
                           r.RoleName, u.IsActive, u.CreatedAt, u.PasswordHash
                    FROM Users u
                    INNER JOIN Roles r ON u.RoleID = r.RoleID
                    WHERE u.Username=@u AND u.IsActive=1", conn);
                cmd.Parameters.AddWithValue("@u", username);

                conn.Open();
                using var rd = cmd.ExecuteReader();
                if (!rd.Read())
                {
                    return null;
                }

                var storedHash = rd["PasswordHash"] as string;
                if (!PasswordHelper.VerifyPassword(password, storedHash))
                {
                    return null;
                }

                var user = MapUser(rd);
                var userId = user.UserID;
                var needsUpgrade = PasswordHelper.IsLegacyHash(storedHash);
                rd.Close();

                if (needsUpgrade)
                {
                    using var updateCmd = new SqlCommand("UPDATE Users SET PasswordHash=@hash WHERE UserID=@id", conn);
                    updateCmd.Parameters.AddWithValue("@hash", PasswordHelper.HashPassword(password));
                    updateCmd.Parameters.AddWithValue("@id", userId);
                    updateCmd.ExecuteNonQuery();
                    passwordUpgraded = true;
                    _logger.LogInformation("Đã nâng cấp password hash lên BCrypt cho user {UserId}", userId);
                }

                return user;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi database khi kiểm tra đăng nhập cho user {Username}", username);
                throw new Exception("Lỗi database khi kiểm tra đăng nhập", ex);
            }
        }

        public int RegisterUser(User u, string plainPassword)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"
                    INSERT INTO Users(Username, PasswordHash, FullName, Email, Phone, RoleID, IsActive)
                    VALUES (@un, @pw, @fn, @em, @ph, @role, 1);
                    SELECT SCOPE_IDENTITY();", conn);

                cmd.Parameters.AddWithValue("@un", u.Username);
                cmd.Parameters.AddWithValue("@pw", PasswordHelper.HashPassword(plainPassword));
                cmd.Parameters.AddWithValue("@fn", u.FullName);
                cmd.Parameters.AddWithValue("@em", u.Email);
                cmd.Parameters.AddWithValue("@ph", (object?)u.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@role", u.RoleID);

                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi database khi đăng ký người dùng {Username}", u.Username);
                throw new Exception("Lỗi database khi đăng ký người dùng", ex);
            }
        }

        public User? GetUserByID(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"
                    SELECT u.UserID, u.Username, u.FullName, u.Email, u.Phone, u.RoleID,
                           r.RoleName, u.IsActive, u.CreatedAt
                    FROM Users u
                    INNER JOIN Roles r ON u.RoleID = r.RoleID
                    WHERE u.UserID=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);

                conn.Open();
                using var rd = cmd.ExecuteReader();
                return rd.Read() ? MapUser(rd) : null;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi database khi lấy người dùng ID {UserId}", id);
                throw new Exception($"Lỗi database khi lấy người dùng ID {id}", ex);
            }
        }

        public User? GetUserByEmail(string email)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"
                    SELECT u.UserID, u.Username, u.FullName, u.Email, u.Phone, u.RoleID,
                           r.RoleName, u.IsActive, u.CreatedAt
                    FROM Users u
                    INNER JOIN Roles r ON u.RoleID = r.RoleID
                    WHERE u.Email = @email", conn);
                cmd.Parameters.AddWithValue("@email", email);

                conn.Open();
                using var rd = cmd.ExecuteReader();
                return rd.Read() ? MapUser(rd) : null;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi database khi lấy người dùng theo email {Email}", email);
                throw new Exception("Lỗi database khi lấy người dùng theo email", ex);
            }
        }

        public List<User> GetAllEmployees()
        {
            try
            {
                var list = new List<User>();
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"
                    SELECT u.UserID, u.Username, u.FullName, u.Email, u.Phone, u.RoleID,
                           r.RoleName, u.IsActive, u.CreatedAt
                    FROM Users u
                    INNER JOIN Roles r ON u.RoleID = r.RoleID
                    WHERE r.RoleName IN (N'Admin', N'Employee')", conn);

                conn.Open();
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    list.Add(MapUser(rd));
                }

                return list;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi database khi lấy danh sách nhân viên");
                throw new Exception("Lỗi database khi lấy danh sách nhân viên", ex);
            }
        }

        public int InsertEmployee(User u, string plainPassword, bool isAdmin = false)
        {
            try
            {
                u.RoleID = isAdmin ? Roles.Admin : Roles.Employee;
                return RegisterUser(u, plainPassword);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi database khi thêm nhân viên {Username}", u.Username);
                throw new Exception("Lỗi database khi thêm nhân viên", ex);
            }
        }

        public bool UpdateEmployee(User u, bool isAdmin)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"
                    UPDATE Users
                       SET FullName = @fn,
                           Email = @em,
                           Phone = @ph,
                           RoleID = @role
                     WHERE UserID = @id", conn);

                cmd.Parameters.AddWithValue("@fn", u.FullName);
                cmd.Parameters.AddWithValue("@em", u.Email);
                cmd.Parameters.AddWithValue("@ph", (object?)u.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@role", isAdmin ? Roles.Admin : Roles.Employee);
                cmd.Parameters.AddWithValue("@id", u.UserID);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi database khi cập nhật nhân viên {UserId}", u.UserID);
                throw new Exception("Lỗi database khi cập nhật nhân viên", ex);
            }
        }

        public bool SetUserActiveState(int userId, bool isActive)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand("UPDATE Users SET IsActive=@active WHERE UserID=@id", conn);
                cmd.Parameters.AddWithValue("@active", isActive);
                cmd.Parameters.AddWithValue("@id", userId);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi database khi cập nhật trạng thái user {UserId}", userId);
                throw new Exception("Lỗi database khi cập nhật trạng thái người dùng", ex);
            }
        }

        public List<User> GetAllCustomers()
        {
            try
            {
                var list = new List<User>();
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"
                    SELECT u.UserID, u.Username, u.FullName, u.Email, u.Phone, u.RoleID,
                           r.RoleName, u.IsActive, u.CreatedAt
                    FROM Users u
                    INNER JOIN Roles r ON u.RoleID = r.RoleID
                    WHERE u.RoleID = @role", conn);

                cmd.Parameters.AddWithValue("@role", Roles.Customer);

                conn.Open();
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    list.Add(MapUser(rd));
                }

                return list;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi database khi lấy danh sách khách hàng");
                throw new Exception("Lỗi database khi lấy danh sách khách hàng", ex);
            }
        }

        public bool UpdateUserProfile(User u)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"UPDATE Users SET FullName=@fn, Email=@em, Phone=@ph WHERE UserID=@id", conn);

                cmd.Parameters.AddWithValue("@fn", u.FullName);
                cmd.Parameters.AddWithValue("@em", u.Email);
                cmd.Parameters.AddWithValue("@ph", (object?)u.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", u.UserID);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi database khi cập nhật thông tin người dùng {UserId}", u.UserID);
                throw new Exception("Lỗi database khi cập nhật thông tin người dùng", ex);
            }
        }

        public bool ChangePassword(int userId, string currentPassword, string newPassword)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var getCmd = new SqlCommand("SELECT PasswordHash FROM Users WHERE UserID=@id", conn);
                getCmd.Parameters.AddWithValue("@id", userId);

                conn.Open();
                var storedHash = getCmd.ExecuteScalar() as string;
                if (storedHash == null || !PasswordHelper.VerifyPassword(currentPassword, storedHash))
                {
                    return false;
                }

                using var updateCmd = new SqlCommand("UPDATE Users SET PasswordHash=@hash WHERE UserID=@id", conn);
                updateCmd.Parameters.AddWithValue("@hash", PasswordHelper.HashPassword(newPassword));
                updateCmd.Parameters.AddWithValue("@id", userId);

                return updateCmd.ExecuteNonQuery() > 0;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi database khi đổi mật khẩu cho user {UserId}", userId);
                throw new Exception("Lỗi database khi đổi mật khẩu", ex);
            }
        }

        public string CreatePasswordResetToken(int userId, TimeSpan validFor)
        {
            try
            {
                var token = Guid.NewGuid().ToString("N");
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"
                    INSERT INTO PasswordResetTokens(UserID, Token, ExpiresAt, CreatedAt)
                    VALUES(@userId, @token, @expires, GETUTCDATE());", conn);

                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@token", token);
                cmd.Parameters.AddWithValue("@expires", DateTime.UtcNow.Add(validFor));

                conn.Open();
                cmd.ExecuteNonQuery();
                return token;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi database khi tạo token reset password cho user {UserId}", userId);
                throw new Exception("Lỗi database khi tạo token reset password", ex);
            }
        }

        public User? GetUserByResetToken(string token)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"
                    SELECT u.UserID, u.Username, u.FullName, u.Email, u.Phone, u.RoleID,
                           r.RoleName, u.IsActive, u.CreatedAt
                    FROM PasswordResetTokens t
                    INNER JOIN Users u ON t.UserID = u.UserID
                    INNER JOIN Roles r ON u.RoleID = r.RoleID
                    WHERE t.Token = @token AND t.UsedAt IS NULL AND t.ExpiresAt > GETUTCDATE();", conn);

                cmd.Parameters.AddWithValue("@token", token);

                conn.Open();
                using var rd = cmd.ExecuteReader();
                return rd.Read() ? MapUser(rd) : null;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi database khi xác thực token reset password");
                throw new Exception("Lỗi database khi xác thực token reset password", ex);
            }
        }

        public bool ResetPasswordWithToken(string token, string newPassword)
        {
            using var conn = new SqlConnection(_connStr);
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {
                using var getCmd = new SqlCommand(@"
                    SELECT UserID FROM PasswordResetTokens
                    WHERE Token = @token AND UsedAt IS NULL AND ExpiresAt > GETUTCDATE();", conn, tran);
                getCmd.Parameters.AddWithValue("@token", token);

                var userIdObj = getCmd.ExecuteScalar();
                if (userIdObj == null)
                {
                    return false;
                }

                var userId = Convert.ToInt32(userIdObj);

                using (var updateUser = new SqlCommand("UPDATE Users SET PasswordHash=@hash WHERE UserID=@id", conn, tran))
                {
                    updateUser.Parameters.AddWithValue("@hash", PasswordHelper.HashPassword(newPassword));
                    updateUser.Parameters.AddWithValue("@id", userId);
                    updateUser.ExecuteNonQuery();
                }

                using (var updateToken = new SqlCommand("UPDATE PasswordResetTokens SET UsedAt = GETUTCDATE() WHERE Token = @token", conn, tran))
                {
                    updateToken.Parameters.AddWithValue("@token", token);
                    updateToken.ExecuteNonQuery();
                }

                tran.Commit();
                return true;
            }
            catch (SqlException ex)
            {
                tran.Rollback();
                _logger.LogError(ex, "Lỗi database khi reset password bằng token");
                throw new Exception("Lỗi database khi reset password", ex);
            }
        }

        private static User MapUser(SqlDataReader rd) => new()
        {
            UserID = rd.GetInt32(rd.GetOrdinal("UserID")),
            Username = rd.GetString(rd.GetOrdinal("Username")),
            FullName = rd.GetString(rd.GetOrdinal("FullName")),
            Email = rd.GetString(rd.GetOrdinal("Email")),
            Phone = rd["Phone"] as string,
            RoleID = rd.GetInt32(rd.GetOrdinal("RoleID")),
            RoleName = rd.GetString(rd.GetOrdinal("RoleName")),
            IsActive = rd.GetBoolean(rd.GetOrdinal("IsActive")),
            CreatedAt = rd.GetDateTime(rd.GetOrdinal("CreatedAt"))
        };

        public int CountUsers()
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Users", conn);
                conn.Open();
                return (int)cmd.ExecuteScalar();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi database khi đếm người dùng");
                throw new Exception("Lỗi database khi đếm người dùng", ex);
            }
        }

        public int CountEmployees()
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand($"SELECT COUNT(*) FROM dbo.Users WHERE RoleID IN ({Roles.Admin},{Roles.Employee})", conn);
                conn.Open();
                return (int)cmd.ExecuteScalar();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi database khi đếm nhân viên");
                throw new Exception("Lỗi database khi đếm nhân viên", ex);
            }
        }

        public int CountCustomers()
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand($"SELECT COUNT(*) FROM dbo.Users WHERE RoleID = {Roles.Customer}", conn);
                conn.Open();
                return (int)cmd.ExecuteScalar();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi database khi đếm khách hàng");
                throw new Exception("Lỗi database khi đếm khách hàng", ex);
            }
        }
    }
}
