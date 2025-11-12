using System;
using System.Collections.Generic;
using Banhang.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Banhang.Data
{
    public class CategoryDAO
    {
        private readonly string _connStr;
        private readonly ILogger<CategoryDAO> _logger;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "AllCategories";

        public CategoryDAO(string connStr, ILogger<CategoryDAO> logger, IMemoryCache cache)
        {
            _connStr = connStr;
            _logger = logger;
            _cache = cache;
        }

        public List<Category> GetAllCategories()
        {
            if (_cache.TryGetValue(CacheKey, out List<Category> cachedCategories))
            {
                return cachedCategories;
            }

            try
            {
                var list = new List<Category>();
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"
                    SELECT CategoryID, CategoryName, [Description]
                    FROM Categories
                    ORDER BY CategoryName", conn);

                conn.Open();
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    list.Add(new Category
                    {
                        CategoryID = rd.GetInt32(rd.GetOrdinal("CategoryID")),
                        CategoryName = rd.GetString(rd.GetOrdinal("CategoryName")),
                        Description = rd["Description"] as string
                    });
                }

                _cache.Set(CacheKey, list, TimeSpan.FromHours(1));
                return list;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi database khi lấy danh mục sản phẩm");
                throw new Exception("Lỗi database khi lấy danh mục", ex);
            }
        }

        public Category? GetCategoryById(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"
                    SELECT CategoryID, CategoryName, [Description]
                    FROM Categories
                    WHERE CategoryID = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);

                conn.Open();
                using var rd = cmd.ExecuteReader();
                return rd.Read()
                    ? new Category
                    {
                        CategoryID = rd.GetInt32(rd.GetOrdinal("CategoryID")),
                        CategoryName = rd.GetString(rd.GetOrdinal("CategoryName")),
                        Description = rd["Description"] as string
                    }
                    : null;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi database khi lấy danh mục {CategoryId}", id);
                throw new Exception("Lỗi database khi lấy danh mục", ex);
            }
        }

        public int CreateCategory(Category category)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"
                    INSERT INTO Categories(CategoryName, [Description])
                    VALUES(@name, @description);
                    SELECT SCOPE_IDENTITY();", conn);

                cmd.Parameters.AddWithValue("@name", category.CategoryName);
                cmd.Parameters.AddWithValue("@description", (object?)category.Description ?? DBNull.Value);

                conn.Open();
                var id = Convert.ToInt32(cmd.ExecuteScalar());
                InvalidateCache();
                return id;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi database khi tạo danh mục {CategoryName}", category.CategoryName);
                throw new Exception("Lỗi database khi tạo danh mục", ex);
            }
        }

        public bool UpdateCategory(Category category)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(@"
                    UPDATE Categories
                       SET CategoryName = @name,
                           [Description] = @description
                     WHERE CategoryID = @id", conn);

                cmd.Parameters.AddWithValue("@name", category.CategoryName);
                cmd.Parameters.AddWithValue("@description", (object?)category.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", category.CategoryID);

                conn.Open();
                var result = cmd.ExecuteNonQuery() > 0;
                if (result)
                {
                    InvalidateCache();
                }

                return result;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi database khi cập nhật danh mục {CategoryId}", category.CategoryID);
                throw new Exception("Lỗi database khi cập nhật danh mục", ex);
            }
        }

        public bool DeleteCategory(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                using var cmd = new SqlCommand("DELETE FROM Categories WHERE CategoryID = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);

                conn.Open();
                var result = cmd.ExecuteNonQuery() > 0;
                if (result)
                {
                    InvalidateCache();
                }

                return result;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi database khi xóa danh mục {CategoryId}", id);
                throw new Exception("Lỗi database khi xóa danh mục", ex);
            }
        }

        private void InvalidateCache()
        {
            _cache.Remove(CacheKey);
        }
    }
}
