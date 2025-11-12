using System;
using System.Collections.Generic;
using System.Data;
using Banhang.Models;
using Microsoft.Data.SqlClient;

namespace Banhang.Data
{
    public class ProductDAO
    {
        private readonly string _connStr;
        public ProductDAO(string connStr) => _connStr = connStr;

        public List<Product> GetAllProducts()
        {
            try
            {
                var list = new List<Product>();
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"
                    SELECT p.ProductID, p.ProductName, p.Price, p.Description, p.ImageUrl,
                           p.Color, p.Size, p.Stock, p.CategoryID, p.IsActive, p.CreatedAt,
                           c.CategoryName
                    FROM Products p
                    INNER JOIN Categories c ON p.CategoryID = c.CategoryID
                    WHERE p.IsActive = 1
                    ORDER BY p.CreatedAt DESC", conn);
                conn.Open();
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    list.Add(MapProduct(rd));
                }

                return list;
            }
            catch (SqlException ex)
            {
                throw new Exception("Lỗi database khi lấy danh sách sản phẩm", ex);
            }
        }

        public PaginatedList<Product> GetProductsPaginated(int pageIndex = 1, int pageSize = 10)
        {
            try
            {
                var list = new List<Product>();
                using var conn = new SqlConnection(_connStr);

                using var countCmd = new SqlCommand("SELECT COUNT(*) FROM Products WHERE IsActive = 1", conn);
                conn.Open();
                var totalCount = (int)countCmd.ExecuteScalar();

                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                pageIndex = Math.Max(1, Math.Min(pageIndex, Math.Max(1, totalPages)));

                using var cmd = new SqlCommand(@"
                    SELECT p.ProductID, p.ProductName, p.Price, p.Description, p.ImageUrl,
                           p.Color, p.Size, p.Stock, p.CategoryID, p.IsActive, p.CreatedAt,
                           c.CategoryName
                    FROM Products p
                    INNER JOIN Categories c ON p.CategoryID = c.CategoryID
                    WHERE p.IsActive = 1
                    ORDER BY p.CreatedAt DESC
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY", conn);
                cmd.Parameters.AddWithValue("@offset", (pageIndex - 1) * pageSize);
                cmd.Parameters.AddWithValue("@pageSize", pageSize);

                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    list.Add(MapProduct(rd));
                }

                return new PaginatedList<Product>(list, totalCount, pageIndex, pageSize);
            }
            catch (SqlException ex)
            {
                throw new Exception("Lỗi database khi lấy sản phẩm phân trang", ex);
            }
        }

        public Product? GetProductByID(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"
                    SELECT p.ProductID, p.ProductName, p.Price, p.Description, p.ImageUrl,
                           p.Color, p.Size, p.Stock, p.CategoryID, p.IsActive, p.CreatedAt,
                           c.CategoryName
                    FROM Products p
                    INNER JOIN Categories c ON p.CategoryID = c.CategoryID
                    WHERE p.ProductID = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                using var rd = cmd.ExecuteReader();
                return rd.Read() ? MapProduct(rd) : null;
            }
            catch (SqlException ex)
            {
                throw new Exception($"Lỗi database khi lấy sản phẩm ID {id}", ex);
            }
        }

        public List<Product> GetProductsByName(string keyword)
        {
            try
            {
                var list = new List<Product>();
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"
                    SELECT p.ProductID, p.ProductName, p.Price, p.Description, p.ImageUrl,
                           p.Color, p.Size, p.Stock, p.CategoryID, p.IsActive, p.CreatedAt,
                           c.CategoryName
                    FROM Products p
                    INNER JOIN Categories c ON p.CategoryID = c.CategoryID
                    WHERE p.IsActive = 1 AND (p.ProductName LIKE @kw OR CAST(p.Price AS NVARCHAR(50)) LIKE @kw)", conn);
                cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
                conn.Open();
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    list.Add(MapProduct(rd));
                }

                return list;
            }
            catch (SqlException ex)
            {
                throw new Exception("Lỗi database khi tìm kiếm sản phẩm", ex);
            }
        }

        public int InsertProduct(Product p)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"
                    INSERT INTO Products(ProductName, Price, Description, ImageUrl, Color, Size, Stock, CategoryID, IsActive)
                    VALUES (@name, @price, @desc, @img, @color, @size, @stock, @cat, @act);
                    SELECT SCOPE_IDENTITY();", conn);

                cmd.Parameters.AddWithValue("@name", p.ProductName);
                cmd.Parameters.AddWithValue("@price", p.Price);
                cmd.Parameters.AddWithValue("@desc", (object?)p.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@img", (object?)p.ImageUrl ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@color", (object?)p.Color ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@size", (object?)p.Size ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@stock", p.Stock);
                cmd.Parameters.AddWithValue("@cat", p.CategoryID);
                cmd.Parameters.AddWithValue("@act", p.IsActive);

                conn.Open();
                var id = cmd.ExecuteScalar();
                return Convert.ToInt32(id);
            }
            catch (SqlException ex)
            {
                throw new Exception("Lỗi database khi thêm sản phẩm", ex);
            }
        }

        public bool UpdateProduct(Product p)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"
                    UPDATE Products
                       SET ProductName=@name, Price=@price, Description=@desc, ImageUrl=@img,
                           Color=@color, Size=@size, Stock=@stock, CategoryID=@cat, IsActive=@act
                     WHERE ProductID=@id", conn);

                cmd.Parameters.AddWithValue("@name", p.ProductName);
                cmd.Parameters.AddWithValue("@price", p.Price);
                cmd.Parameters.AddWithValue("@desc", (object?)p.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@img", (object?)p.ImageUrl ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@color", (object?)p.Color ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@size", (object?)p.Size ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@stock", p.Stock);
                cmd.Parameters.AddWithValue("@cat", p.CategoryID);
                cmd.Parameters.AddWithValue("@act", p.IsActive);
                cmd.Parameters.AddWithValue("@id", p.ProductID);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (SqlException ex)
            {
                throw new Exception($"Lỗi database khi cập nhật sản phẩm ID {p.ProductID}", ex);
            }
        }

        public bool DeleteProduct(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand("DELETE FROM Products WHERE ProductID=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (SqlException ex)
            {
                throw new Exception($"Lỗi database khi xóa sản phẩm ID {id}", ex);
            }
        }

        public int CountProducts()
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Products", conn);
                conn.Open();
                return (int)cmd.ExecuteScalar();
            }
            catch (SqlException ex)
            {
                throw new Exception("Lỗi database khi đếm sản phẩm", ex);
            }
        }

        public int CountProductsByCategory(int categoryId)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Products WHERE CategoryID=@cid", conn);
                cmd.Parameters.AddWithValue("@cid", categoryId);
                conn.Open();
                return (int)cmd.ExecuteScalar();
            }
            catch (SqlException ex)
            {
                throw new Exception($"Lỗi database khi đếm sản phẩm theo danh mục {categoryId}", ex);
            }
        }

        public decimal GetTotalRevenue()
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                var sql = @"
                    SELECT ISNULL(SUM(cd.Quantity * p.Price), 0)
                    FROM dbo.CartDetails cd
                    JOIN dbo.Carts c ON c.CartID = cd.CartID
                    JOIN dbo.Products p ON p.ProductID = cd.ProductID
                    WHERE c.[Status] = 1";
                using var cmd = new SqlCommand(sql, conn);
                conn.Open();
                var result = cmd.ExecuteScalar();
                return result == DBNull.Value ? 0m : Convert.ToDecimal(result);
            }
            catch (SqlException ex)
            {
                throw new Exception("Lỗi database khi tính tổng doanh thu", ex);
            }
        }

        private static Product MapProduct(IDataRecord rd) => new()
        {
            ProductID = rd.GetInt32(rd.GetOrdinal("ProductID")),
            ProductName = rd.GetString(rd.GetOrdinal("ProductName")),
            Price = rd.GetDecimal(rd.GetOrdinal("Price")),
            Description = rd["Description"] as string,
            ImageUrl = rd["ImageUrl"] as string,
            Color = rd["Color"] as string,
            Size = rd["Size"] as string,
            Stock = rd.GetInt32(rd.GetOrdinal("Stock")),
            CategoryID = rd.GetInt32(rd.GetOrdinal("CategoryID")),
            IsActive = rd.GetBoolean(rd.GetOrdinal("IsActive")),
            CreatedAt = rd.GetDateTime(rd.GetOrdinal("CreatedAt")),
            CategoryName = rd.GetString(rd.GetOrdinal("CategoryName"))
        };
    }
}
