using doantotnghiep.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace doantotnghiep.Controllers
{
    public class OrderDetailController : Controller
    {
        readonly FreshFruitDBEntities db = new FreshFruitDBEntities();


        public ActionResult Index()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index", "LoginSignup");
            }

            int userId = (int)Session["UserID"];

            // Lấy danh sách đơn hàng theo MaKH và include ChiTietDonHang
            var donHangs = db.DonHangs
                .Where(dh => dh.MaKH == userId)
                .OrderByDescending(dh => dh.NgayTao)
                .ToList();
             return View(donHangs);
        }
            


    }
}
