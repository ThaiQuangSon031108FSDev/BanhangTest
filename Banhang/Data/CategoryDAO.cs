using System;
using System.Collections.Generic;
using Banhang.Models;
using Microsoft.Data.SqlClient;

namespace Banhang.Data
{
    public class CategoryDAO
    {
        private readonly string _connStr;
        public CategoryDAO(string connStr) => _connStr = connStr;

        public List<Category> GetAllCategories()
        {
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

                return list;
            }
            catch (SqlException ex)
            {
                throw new Exception("Lỗi database khi lấy danh mục", ex);
            }
        }
    }
}
