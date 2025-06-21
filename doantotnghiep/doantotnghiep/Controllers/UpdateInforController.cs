using System;
using System.Linq;
using System.Web.Mvc;
using doantotnghiep.Models;

namespace doantotnghiep.Controllers
{
    public class UpdateInforController : Controller
    {
        FreshFruitDBEntities db = new FreshFruitDBEntities();

        // GET: UpdateInfor
        [HttpGet]
        public ActionResult Index()
        {
            if (Session["UserName"] != null)
            {
                string username = Session["UserName"].ToString();
                var khachHang = db.Account_KhachHang.FirstOrDefault(x => x.TaiKhoan == username);
                return View(khachHang);
            }
            return RedirectToAction("Index", "LoginSignup");
        }

        // POST: UpdateInfor
        [HttpPost]
        public ActionResult Index(Account_KhachHang model)
        {
            var khachHang = db.Account_KhachHang.Find(model.AccountID);
            if (khachHang != null)
            {
                khachHang.TenKH = model.TenKH;
                khachHang.Diachi = model.Diachi;
                khachHang.Ngaysinh = model.Ngaysinh;
                khachHang.Phone = model.Phone;

                db.SaveChanges();
                ViewBag.Message = "Cập nhật thành công!";
            }
            return View(khachHang);
        }
    }
}
