using System;
using System.Linq;
using System.Web.Mvc;
using doantotnghiep.Models;

namespace doantotnghiep.Areas.Admin.Controllers
{
    public class AdminNhapHuyHangController : Controller
    {
        readonly FreshFruitDBEntities db = new FreshFruitDBEntities();

        // ================= NHẬP HÀNG ==================

        public ActionResult NhapHang()
        {
            ViewBag.MaSP = new SelectList(db.SanPhams, "MaSP", "TenSP");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult NhapHang(NhapHang phieuNhap)
        {
            if (ModelState.IsValid)
            {
                var sanPham = db.SanPhams.Find(phieuNhap.MaSP);
                if (sanPham == null)
                {
                    ModelState.AddModelError("MaSP", "Sản phẩm không tồn tại.");
                }
                else if (phieuNhap.SoLuongNhap <= 0)
                {
                    ModelState.AddModelError("SoLuongNhap", "Số lượng nhập phải lớn hơn 0.");
                }
                else
                {
                    phieuNhap.NgayNhap = DateTime.Now;
                    db.NhapHangs.Add(phieuNhap);

                    sanPham.TonKho += phieuNhap.SoLuongNhap;

                    db.SaveChanges();
                    TempData["Success"] = "Nhập hàng thành công!";
                    return RedirectToAction("NhapHang");
                }
            }

            ViewBag.MaSP = new SelectList(db.SanPhams, "MaSP", "TenSP", phieuNhap.MaSP);
            return View(phieuNhap);
        }

        // ================= HỦY HÀNG ==================

        public ActionResult HuyHang()
        {
            ViewBag.MaSP = new SelectList(db.SanPhams, "MaSP", "TenSP");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HuyHang(HuyHang phieuHuy)
        {
            if (ModelState.IsValid)
            {
                var sanPham = db.SanPhams.Find(phieuHuy.MaSP);
                if (sanPham == null)
                {
                    ModelState.AddModelError("MaSP", "Sản phẩm không tồn tại.");
                }
                else if (phieuHuy.SoLuongHuy <= 0)
                {
                    ModelState.AddModelError("SoLuongHuy", "Số lượng hủy phải lớn hơn 0.");
                }
                else if (phieuHuy.SoLuongHuy > sanPham.TonKho)
                {
                    ModelState.AddModelError("SoLuongHuy", $"Số lượng hủy ({phieuHuy.SoLuongHuy}) vượt tồn kho hiện tại ({sanPham.TonKho}).");
                }
                else
                {
                    phieuHuy.NgayHuy = DateTime.Now;
                    db.HuyHangs.Add(phieuHuy);

                    sanPham.TonKho -= phieuHuy.SoLuongHuy;

                    db.SaveChanges();
                    TempData["Success"] = "Hủy hàng thành công!";
                    return RedirectToAction("HuyHang");
                }
            }

            ViewBag.MaSP = new SelectList(db.SanPhams, "MaSP", "TenSP", phieuHuy.MaSP);
            return View(phieuHuy);
        }
    }
}
