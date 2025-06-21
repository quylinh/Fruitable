using doantotnghiep.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace doantotnghiep.Areas.Admin.Controllers
{
    public class AdminQuanLyDonHangController : Controller
    {
        readonly FreshFruitDBEntities db = new FreshFruitDBEntities();

        // GET: Admin/AdminQuanLyDonHang
        public ActionResult Index()
        {
            if (Session["UserID"] == null || Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Index", "LoginSignup", new { area = "" });
            }
            var donHangList = db.DonHangs.OrderByDescending(d => d.NgayTao).ToList();

            return View(donHangList);
        }
        public ActionResult Detail()
        {

            return View();
        }
        public ActionResult Update()
        {

            return View();
        }
        public ActionResult Delete()
        {

            return View();
        }
    }
}