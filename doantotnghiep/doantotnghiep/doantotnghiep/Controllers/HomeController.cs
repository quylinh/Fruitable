using doantotnghiep.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace doantotnghiep.Controllers
{
    public class HomeController : Controller
    {
        readonly FreshFruitDBEntities db = new FreshFruitDBEntities();

        // Trang chủ hiển thị danh sách sản phẩm
        public ActionResult Index()
        {
            var dsSanPham = db.SanPhams.ToList();

            // Tính Session["SoLuong"] nếu đã đăng nhập
            if (Session["UserID"] != null)
            {
                int userId = (int)Session["UserID"];
                var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == userId);
                int soLuong = 0;
                if (gioHang != null)
                {
                    soLuong = db.ChiTietGioHangs
                                .Where(c => c.MaGioHang == gioHang.MaGioHang)
                                .Sum(c => (int?)c.SoLuong) ?? 0; // tránh null
                }
                Session["SoLuong"] = soLuong;
            }
            else
            {
                Session["SoLuong"] = 0;
            }
            return View(dsSanPham);
        }

        [HttpPost]
        public JsonResult AddToCart(int id)
        {
            if (Session["UserID"] == null)
            {
                // Trả về URL cần chuyển hướng khi chưa đăng nhập
                return Json(new { success = false, redirectUrl = Url.Action("Index", "LoginSignUp") });
            }

            int userId = (int)Session["UserID"];
            var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == userId);
            if (gioHang == null)
            {
                gioHang = new GioHang
                {
                    MaKH = userId,
                    NgayTao = DateTime.Now
                };
                db.GioHangs.Add(gioHang);
                db.SaveChanges();
            }

            var sanPham = db.SanPhams.Find(id);
            if (sanPham == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại" });
            }

            var chiTiet = db.ChiTietGioHangs
                .FirstOrDefault(c => c.MaGioHang == gioHang.MaGioHang && c.MaSP == id);

            if (chiTiet != null)
            {
                chiTiet.SoLuong += 1;
            }
            else
            {
                chiTiet = new ChiTietGioHang
                {
                    MaGioHang = gioHang.MaGioHang,
                    MaSP = id,
                    SoLuong = 1,
                    NgayThem = DateTime.Now
                };
                db.ChiTietGioHangs.Add(chiTiet);
            }

            db.SaveChanges();

            // Đếm lại tổng số sản phẩm trong giỏ hàng và cập nhật Session["SoLuong"]
            int soLuong = db.ChiTietGioHangs
                           .Where(c => c.MaGioHang == gioHang.MaGioHang)
                           .Sum(c => (int?)c.SoLuong) ?? 0;
            Session["SoLuong"] = soLuong;

            return Json(new { success = true, message = "Đã thêm vào giỏ hàng", newCount = soLuong });
        }

        public PartialViewResult TopSanPham()
        {
            // Lấy top 5 sản phẩm được mua nhiều nhất từ bảng DonHang và ChiTietDonHang
            var topSanPhamIds = db.ChiTietDonHangs
                .Join(db.DonHangs,
                      ctdh => ctdh.MaDH,
                      dh => dh.MaDH,
                      (ctdh, dh) => new { ctdh, dh })
                .Where(x => x.dh.TrangThai == "Đã giao") // Chỉ tính đơn hàng đã giao
                .GroupBy(x => x.ctdh.MaSP)
                .Select(g => new {
                    MaSP = g.Key,
                    TongSoLuong = g.Sum(x => x.ctdh.SoLuong)
                })
                .OrderByDescending(g => g.TongSoLuong)
                .Take(5)
                .Select(g => g.MaSP)
                .ToList();
            // Lấy thông tin sản phẩm tương ứng
            var topSanPham = db.SanPhams
                .Where(sp => topSanPhamIds.Contains(sp.MaSP))
                .OrderBy(sp => topSanPhamIds.IndexOf(sp.MaSP)) // Giữ thứ tự theo số lượng bán
                .ToList();

            // Nếu không có sản phẩm nào được bán, lấy 5 sản phẩm mới nhất
            if (!topSanPham.Any())
            {
                topSanPham = db.SanPhams
                    .OrderByDescending(sp => sp.NgayNhap) // Giả sử có trường NgayTao
                    .Take(5)
                    .ToList();
            }
            return PartialView("_TopSanPham", topSanPham);
        }

        // Tìm kiếm với AJAX để cuộn mượt mà
        [HttpPost]
        public JsonResult TimKiemAjax(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return Json(new { success = false, message = "Vui lòng nhập từ khóa tìm kiếm" });
            }

            var ketQua = db.SanPhams
                .Where(sp => sp.TenSP.Contains(keyword) || sp.MotaSP.Contains(keyword))
                .Select(sp => new {
                    MaSP = sp.MaSP,
                    TenSP = sp.TenSP,
                    GiaSP = sp.GiaSP,
                    HinhAnh = sp.AnhSP,
                    MotaSP = sp.MotaSP
                })
                .ToList();

            if (ketQua.Any())
            {
                return Json(new
                {
                    success = true,
                    products = ketQua,
                    message = $"Tìm thấy {ketQua.Count} sản phẩm cho từ khóa '{keyword}'"
                });
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = $"Không tìm thấy sản phẩm nào cho từ khóa '{keyword}'"
                });
            }
        }

        // Giữ lại phương thức GET để hỗ trợ URL trực tiếp
        [HttpGet]
        public ActionResult TimKiem(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return RedirectToAction("Index");
            }

            var ketQua = db.SanPhams
                .Where(sp => sp.TenSP.Contains(keyword) || sp.MotaSP.Contains(keyword))
                .ToList();

            ViewBag.TuKhoa = keyword;
            ViewBag.IsSearchResult = true;
            ViewBag.SoKetQua = ketQua.Count();

            return View("Index", ketQua);
        }

        // Giữ lại phương thức POST cho trường hợp fallback
        [HttpPost]
        public ActionResult TimKiem(string keyword, string submitButton)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return RedirectToAction("Index");
            }

            var ketQua = db.SanPhams
                .Where(sp => sp.TenSP.Contains(keyword) || sp.MotaSP.Contains(keyword))
                .ToList();

            ViewBag.TuKhoa = keyword;
            ViewBag.IsSearchResult = true;
            ViewBag.SoKetQua = ketQua.Count();

            return View("Index", ketQua);
        }
    }
}