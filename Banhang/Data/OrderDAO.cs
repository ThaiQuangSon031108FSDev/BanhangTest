using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Banhang.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Banhang.Data
{
    public class OrderDAO
    {
        private readonly string _connStr;
        private readonly ILogger<OrderDAO> _logger;

        public OrderDAO(string connStr, ILogger<OrderDAO> logger)
        {
            _connStr = connStr;
            _logger = logger;
        }

        public int CreateOrder(Order order, List<CartItem> items)
        {
            if (items.Count == 0)
            {
                throw new ArgumentException("Order must contain at least one item", nameof(items));
            }

            using var conn = new SqlConnection(_connStr);
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {
                using var orderCmd = new SqlCommand(@"
                    INSERT INTO Orders (UserID, OrderDate, TotalAmount, Status, ShipName, ShipAddress, ShipPhone, Notes)
                    OUTPUT INSERTED.OrderID
                    VALUES (@userId, @orderDate, @total, @status, @shipName, @shipAddress, @shipPhone, @notes);", conn, tran);

                orderCmd.Parameters.AddWithValue("@userId", order.UserID);
                orderCmd.Parameters.AddWithValue("@orderDate", order.OrderDate);
                orderCmd.Parameters.AddWithValue("@total", order.TotalAmount);
                orderCmd.Parameters.AddWithValue("@status", order.Status);
                orderCmd.Parameters.AddWithValue("@shipName", order.ShipName);
                orderCmd.Parameters.AddWithValue("@shipAddress", order.ShipAddress);
                orderCmd.Parameters.AddWithValue("@shipPhone", order.ShipPhone);
                orderCmd.Parameters.AddWithValue("@notes", (object?)order.Notes ?? DBNull.Value);

                var orderId = Convert.ToInt32(orderCmd.ExecuteScalar());

                foreach (var item in items)
                {
                    using var detailCmd = new SqlCommand(@"
                        INSERT INTO OrderDetails (OrderID, ProductID, Quantity, UnitPrice)
                        VALUES (@orderId, @productId, @qty, @price);", conn, tran);

                    detailCmd.Parameters.AddWithValue("@orderId", orderId);
                    detailCmd.Parameters.AddWithValue("@productId", item.ProductID);
                    detailCmd.Parameters.AddWithValue("@qty", item.Quantity);
                    detailCmd.Parameters.AddWithValue("@price", item.Price);

                    detailCmd.ExecuteNonQuery();

                    using var stockCmd = new SqlCommand(@"
                        UPDATE Products
                           SET Stock = Stock - @qty
                         WHERE ProductID = @productId AND Stock >= @qty;", conn, tran);

                    stockCmd.Parameters.AddWithValue("@qty", item.Quantity);
                    stockCmd.Parameters.AddWithValue("@productId", item.ProductID);

                    var affectedRows = stockCmd.ExecuteNonQuery();
                    if (affectedRows == 0)
                    {
                        throw new Exception($"Không thể cập nhật tồn kho cho sản phẩm {item.ProductID}");
                    }
                }

                tran.Commit();
                return orderId;
            }
            catch (SqlException ex)
            {
                tran.Rollback();
                _logger.LogError(ex, "Lỗi database khi tạo đơn hàng mới cho user {UserId}", order.UserID);
                throw new Exception("Lỗi database khi tạo đơn hàng", ex);
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        public List<Order> GetAllOrders()
        {
            var list = new List<Order>();
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
                SELECT o.OrderID, o.UserID, o.OrderDate, o.TotalAmount, o.Status,
                       o.ShipName, o.ShipAddress, o.ShipPhone, o.Notes,
                       u.Username, u.Email
                FROM Orders o
                LEFT JOIN Users u ON o.UserID = u.UserID
                ORDER BY o.OrderDate DESC;", conn);

            conn.Open();
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(MapOrder(rd));
            }

            return list;
        }

        public List<Order> GetOrdersByUser(int userId)
        {
            var list = new List<Order>();
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
                SELECT o.OrderID, o.UserID, o.OrderDate, o.TotalAmount, o.Status,
                       o.ShipName, o.ShipAddress, o.ShipPhone, o.Notes,
                       u.Username, u.Email
                FROM Orders o
                LEFT JOIN Users u ON o.UserID = u.UserID
                WHERE o.UserID = @userId
                ORDER BY o.OrderDate DESC;", conn);

            cmd.Parameters.AddWithValue("@userId", userId);

            conn.Open();
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(MapOrder(rd));
            }

            return list;
        }

        public Order? GetOrderWithDetails(int orderId)
        {
            using var conn = new SqlConnection(_connStr);
            conn.Open();

            Order? order = null;
            using (var cmd = new SqlCommand(@"
                SELECT o.OrderID, o.UserID, o.OrderDate, o.TotalAmount, o.Status,
                       o.ShipName, o.ShipAddress, o.ShipPhone, o.Notes,
                       u.Username, u.Email
                FROM Orders o
                LEFT JOIN Users u ON o.UserID = u.UserID
                WHERE o.OrderID = @id;", conn))
            {
                cmd.Parameters.AddWithValue("@id", orderId);
                using var rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    order = MapOrder(rd);
                }
            }

            if (order == null)
            {
                return null;
            }

            using (var detailCmd = new SqlCommand(@"
                SELECT od.OrderDetailID, od.OrderID, od.ProductID, od.Quantity, od.UnitPrice,
                       p.ProductName, p.ImageUrl
                FROM OrderDetails od
                INNER JOIN Products p ON od.ProductID = p.ProductID
                WHERE od.OrderID = @id;", conn))
            {
                detailCmd.Parameters.AddWithValue("@id", orderId);
                using var rd = detailCmd.ExecuteReader();
                while (rd.Read())
                {
                    order.Items.Add(new OrderDetail
                    {
                        OrderDetailID = rd.GetInt32(0),
                        OrderID = rd.GetInt32(1),
                        ProductID = rd.GetInt32(2),
                        Quantity = rd.GetInt32(3),
                        UnitPrice = rd.GetDecimal(4),
                        ProductName = rd.GetString(5),
                        ImageUrl = rd.IsDBNull(6) ? null : rd.GetString(6)
                    });
                }
            }

            return order;
        }

        public bool UpdateStatus(int orderId, string newStatus)
        {
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
                UPDATE Orders
                   SET Status = @status
                 WHERE OrderID = @id;", conn);

            cmd.Parameters.AddWithValue("@status", newStatus);
            cmd.Parameters.AddWithValue("@id", orderId);

            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public List<(DateTime Month, decimal Total)> GetMonthlyRevenue(int months)
        {
            var result = new List<(DateTime Month, decimal Total)>();
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
                SELECT DATEFROMPARTS(YEAR(o.OrderDate), MONTH(o.OrderDate), 1) AS MonthStart,
                       SUM(o.TotalAmount) AS TotalAmount
                FROM Orders o
                GROUP BY DATEFROMPARTS(YEAR(o.OrderDate), MONTH(o.OrderDate), 1)
                ORDER BY MonthStart DESC;", conn);

            conn.Open();
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                var monthStart = rd.GetDateTime(0);
                var total = rd.GetDecimal(1);
                result.Add((monthStart, total));
            }

            return result
                .OrderByDescending(r => r.Month)
                .Take(months)
                .OrderBy(r => r.Month)
                .ToList();
        }

        public List<(string ProductName, int Quantity, decimal Revenue)> GetTopProducts(int top)
        {
            var result = new List<(string ProductName, int Quantity, decimal Revenue)>();
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(@"
                SELECT TOP (@top)
                       p.ProductName,
                       SUM(od.Quantity) AS TotalQty,
                       SUM(od.Quantity * od.UnitPrice) AS Revenue
                FROM OrderDetails od
                INNER JOIN Products p ON od.ProductID = p.ProductID
                GROUP BY p.ProductName
                ORDER BY Revenue DESC;", conn);

            cmd.Parameters.AddWithValue("@top", top);

            conn.Open();
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                var name = rd.GetString(0);
                var quantity = rd.GetInt32(1);
                var revenue = rd.GetDecimal(2);
                result.Add((name, quantity, revenue));
            }

            return result;
        }

        private static Order MapOrder(IDataRecord rd)
            => new()
            {
                OrderID = rd.GetInt32(0),
                UserID = rd.GetInt32(1),
                OrderDate = rd.GetDateTime(2),
                TotalAmount = rd.GetDecimal(3),
                Status = rd.GetString(4),
                ShipName = rd.GetString(5),
                ShipAddress = rd.GetString(6),
                ShipPhone = rd.GetString(7),
                Notes = rd.IsDBNull(8) ? null : rd.GetString(8),
                Username = rd.IsDBNull(9) ? null : rd.GetString(9),
                Email = rd.IsDBNull(10) ? null : rd.GetString(10)
            };
    }
}
