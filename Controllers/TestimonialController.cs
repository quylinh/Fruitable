using doantotnghiep.Models;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace doantotnghiep.Controllers
{
    public class TestimonialController : Controller
    {
        private readonly FreshFruitDBEntities db = new FreshFruitDBEntities();

        public ActionResult Index()
        {
            // Lấy danh sách nhận xét có thông tin khách hàng và sản phẩm
            var nhanXets = db.NhanXetSanPhams
                .Include(nx => nx.Account_KhachHang)
                .Include(nx => nx.SanPham)
                .Where(nx => nx.Account_KhachHang != null && nx.SanPham != null)
                .OrderByDescending(nx => nx.NgayNhanXet)
                .ToList();

            return View(nhanXets);
        }
    }
}
