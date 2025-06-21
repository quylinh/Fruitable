using doantotnghiep.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace doantotnghiep.Areas.Admin.Controllers
{
    public class AdminOrderController : Controller
    {
        readonly FreshFruitDBEntities db = new FreshFruitDBEntities();

        public ActionResult Index()
        {
            if (Session["UserID"] == null || Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Index", "LoginSignup", new { area = "" });
            }

            // Lấy top 1000 đơn hàng theo ngày tạo giảm dần
            var donHangs = db.DonHangs
                .OrderByDescending(dh => dh.NgayTao)
                .Take(1000)
                .ToList();

            return View(donHangs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CapNhatTrangThai(int maDH, string trangThaiMoi)
        {
            if (Session["UserID"] == null || Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return Json(new { success = false, message = "Không có quyền truy cập." });
            }

<<<<<<< HEAD
            var donHang = db.DonHangs.Find(maDH);
=======
            var donHang = db.DonHangs
                            .Include("ChiTietDonHangs") // Load luôn danh sách ChiTietDonHang
                            .FirstOrDefault(d => d.MaDH == maDH);

>>>>>>> 997f9b8680ef12397472517faa815b8e795d0eee
            if (donHang != null)
            {
                donHang.TrangThai = trangThaiMoi;

                if (trangThaiMoi == "Hoàn thành")
                {
                    donHang.NgayThanhToan = DateTime.Now;
                    db.Entry(donHang).Property(x => x.NgayThanhToan).IsModified = true;
<<<<<<< HEAD
=======

                    // Cập nhật Ngaygiao cho từng ChiTietDonHang của đơn hàng này
                    foreach (var ct in donHang.ChiTietDonHangs)
                    {
                        ct.Ngaygiao = DateTime.Now;
                        db.Entry(ct).Property(x => x.Ngaygiao).IsModified = true;
                    }
>>>>>>> 997f9b8680ef12397472517faa815b8e795d0eee
                }

                try
                {
                    db.SaveChanges();
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi lưu đơn hàng: " + ex.Message });
                }
            }

            return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
        }

<<<<<<< HEAD
=======

>>>>>>> 997f9b8680ef12397472517faa815b8e795d0eee
    }
}
