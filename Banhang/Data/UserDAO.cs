using System;
using System.Collections.Generic;
using Banhang.Helpers;
using Banhang.Models;
using Microsoft.Data.SqlClient;

namespace Banhang.Data
{
    public class UserDAO
    {
        private readonly string _connStr;
        public UserDAO(string connStr) => _connStr = connStr;

        public User? CheckLogin(string username, string password)
        {
            try
            {
                var hashedPassword = PasswordHelper.HashPassword(password);
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"
                    SELECT u.UserID, u.Username, u.FullName, u.Email, u.Phone, u.RoleID,
                           r.RoleName, u.IsActive, u.CreatedAt
                    FROM Users u
                    INNER JOIN Roles r ON u.RoleID = r.RoleID
                    WHERE u.Username=@u AND u.PasswordHash=@p AND u.IsActive=1", conn);
                cmd.Parameters.AddWithValue("@u", username);
                cmd.Parameters.AddWithValue("@p", hashedPassword);

                conn.Open();
                using var rd = cmd.ExecuteReader();
                return rd.Read() ? MapUser(rd) : null;
            }
            catch (SqlException ex)
            {
                throw new Exception("Lỗi database khi kiểm tra đăng nhập", ex);
            }
        }

        public int RegisterUser(User u, string plainPassword)
        {
            try
            {
                var hashedPassword = PasswordHelper.HashPassword(plainPassword);
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"
                    INSERT INTO Users(Username, PasswordHash, FullName, Email, Phone, RoleID, IsActive)
                    VALUES (@un, @pw, @fn, @em, @ph, @role, 1);
                    SELECT SCOPE_IDENTITY();", conn);

                cmd.Parameters.AddWithValue("@un", u.Username);
                cmd.Parameters.AddWithValue("@pw", hashedPassword);
                cmd.Parameters.AddWithValue("@fn", u.FullName);
                cmd.Parameters.AddWithValue("@em", u.Email);
                cmd.Parameters.AddWithValue("@ph", (object?)u.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@role", u.RoleID);

                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (SqlException ex)
            {
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
                throw new Exception($"Lỗi database khi lấy người dùng ID {id}", ex);
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
                throw new Exception("Lỗi database khi lấy danh sách nhân viên", ex);
            }
        }

        public int InsertEmployee(User u, string plainPassword, bool isAdmin = false)
        {
            try
            {
                u.RoleID = isAdmin ? 1 : 2; // 1-Admin, 2-Employee
                return RegisterUser(u, plainPassword);
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi database khi thêm nhân viên", ex);
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
                throw new Exception("Lỗi database khi đếm người dùng", ex);
            }
        }

        public int CountEmployees()
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Users WHERE RoleID IN (1,2)", conn);
                conn.Open();
                return (int)cmd.ExecuteScalar();
            }
            catch (SqlException ex)
            {
                throw new Exception("Lỗi database khi đếm nhân viên", ex);
            }
        }

        public int CountCustomers()
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Users WHERE RoleID = 3", conn);
                conn.Open();
                return (int)cmd.ExecuteScalar();
            }
            catch (SqlException ex)
            {
                throw new Exception("Lỗi database khi đếm khách hàng", ex);
            }
        }
    }
}
