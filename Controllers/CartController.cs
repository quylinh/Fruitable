using doantotnghiep.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace doantotnghiep.Controllers
{
    public class CartController : Controller
    {
        readonly FreshFruitDBEntities db = new FreshFruitDBEntities();

        // GET: Cart
        public ActionResult Index()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
                return RedirectToAction("Index", "LoginSignup");

            int userId = (int)Session["UserID"];

            // Lấy giỏ hàng của người dùng
            var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == userId);
            if (gioHang == null)
            {
                // Giỏ hàng trống
                Session["SoLuong"] = 0;
                return View(new List<ChiTietGioHang>());
            }

            // Lấy danh sách chi tiết giỏ hàng
            var danhSach = db.ChiTietGioHangs
                             .Where(c => c.MaGioHang == gioHang.MaGioHang)
                             .ToList();

            // Tính tổng tạm tính và tổng cộng
            decimal tongTamTinh = danhSach.Sum(x => x.SoLuong * x.SanPham.GiaSP);
         /*   decimal phiVanChuyen = 3.00m;
            decimal tongCong = tongTamTinh + phiVanChuyen;*/

            // Cập nhật lại số lượng giỏ hàng trong Session
            int soLuong = danhSach.Sum(x => x.SoLuong);
            Session["SoLuong"] = soLuong;

            // Gửi dữ liệu ra View
            ViewBag.TongTamTinh = tongTamTinh;
/*            ViewBag.PhiVanChuyen = phiVanChuyen;
*/            ViewBag.tongTamTinh = tongTamTinh;

            return View(danhSach);
        }


        [HttpPost]
        public JsonResult TangSoLuong(int maSP)
        {
            int userId = (int)Session["UserID"];
            var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == userId);

            var chiTiet = db.ChiTietGioHangs
                            .FirstOrDefault(c => c.MaGioHang == gioHang.MaGioHang && c.MaSP == maSP);

            if (chiTiet != null)
            {
                chiTiet.SoLuong += 1;
                db.SaveChanges();

                decimal thanhTien = chiTiet.SoLuong * chiTiet.SanPham.GiaSP;

                // Cập nhật lại số lượng trong Session
                int soLuong = db.ChiTietGioHangs
                               .Where(c => c.MaGioHang == gioHang.MaGioHang)
                               .Sum(c => c.SoLuong);
                Session["SoLuong"] = soLuong;

                return Json(new { soLuong = chiTiet.SoLuong, thanhTien = thanhTien, newCount = soLuong });
            }

            return Json(new { soLuong = 0, thanhTien = 0, newCount = 0 });
        }


        
        [HttpPost]
        public JsonResult GiamSoLuong(int maSP)
        {
            int userId = (int)Session["UserID"];
            var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == userId);

            var chiTiet = db.ChiTietGioHangs
                            .FirstOrDefault(c => c.MaGioHang == gioHang.MaGioHang && c.MaSP == maSP);

            if (chiTiet != null && chiTiet.SoLuong > 1)
            {
                chiTiet.SoLuong -= 1;
                db.SaveChanges();

                decimal thanhTien = chiTiet.SoLuong * chiTiet.SanPham.GiaSP;

                // Cập nhật lại số lượng trong Session
                int soLuong = db.ChiTietGioHangs
                               .Where(c => c.MaGioHang == gioHang.MaGioHang)
                               .Sum(c => c.SoLuong);
                Session["SoLuong"] = soLuong;

                return Json(new { soLuong = chiTiet.SoLuong, thanhTien = thanhTien, newCount = soLuong });
            }
            else if (chiTiet != null && chiTiet.SoLuong == 1)
            {
                // Khi số lượng = 1, xóa sản phẩm khỏi giỏ hàng
                db.ChiTietGioHangs.Remove(chiTiet);
                db.SaveChanges();

                // Cập nhật lại số lượng trong Session
                int soLuong = db.ChiTietGioHangs
                               .Where(c => c.MaGioHang == gioHang.MaGioHang)
                               .Sum(c => c.SoLuong);
                Session["SoLuong"] = soLuong;

                return Json(new { soLuong = 0, thanhTien = 0, newCount = soLuong });
            }

            return Json(new { soLuong = 0, thanhTien = 0, newCount = 0 });
        }

        // Xóa sản phẩm khỏi giỏ hàng
        [HttpPost]
        public ActionResult XoaSanPham(int maSP)
        {
            int userId = (int)Session["UserID"];
            var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == userId);

            var chiTiet = db.ChiTietGioHangs
                            .FirstOrDefault(c => c.MaGioHang == gioHang.MaGioHang && c.MaSP == maSP);

            if (chiTiet != null)
            {
                db.ChiTietGioHangs.Remove(chiTiet);
                db.SaveChanges();
            }

            // Cập nhật lại số lượng trong Session
            int soLuong = db.ChiTietGioHangs
                  .Where(c => c.MaGioHang == gioHang.MaGioHang)
                  .Sum(c => (int?)c.SoLuong) ?? 0;

            Session["SoLuong"] = soLuong;

            return RedirectToAction("Index");
        }


        [HttpPost]
        public void LuuTamTinh(decimal tamTinh)
        {
            Session["TamTinh"] = tamTinh;
        }




    }
}
