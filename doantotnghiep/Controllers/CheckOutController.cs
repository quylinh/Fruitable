using doantotnghiep.Models;
using Newtonsoft.Json.Linq;
using PayPal.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace doantotnghiep.Controllers
{
    public class CheckOutController : Controller
    {
        readonly FreshFruitDBEntities db = new FreshFruitDBEntities();
        private const string StoreAddress = "33/26 Gò Dầu, Tân Phú, Hồ Chí Minh";
        private const string MapboxToken = "pk.eyJ1IjoibXlhcHAtMjAyNSIsImEiOiJjbWFxcWt5cmMwMm56Mmpwc3QzdGI2bGxtIn0.jhkToWCNM3hhD3KzUlVLXg";

        // Trang giỏ hàng và thanh toán
        public async Task<ActionResult> Index(string customerAddress)
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Index", "LoginSignUp");

            int userId = (int)Session["UserID"];
            var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == userId);
            if (gioHang == null)
            {
                ViewBag.Message = "Giỏ hàng trống.";
                return View(new List<ChiTietGioHang>());
            }

            var chiTiet = db.ChiTietGioHangs.Where(c => c.MaGioHang == gioHang.MaGioHang).ToList();
            decimal tamTinh = chiTiet.Sum(c => c.SanPham.GiaSP * c.SoLuong);

            // Tính phí ship và khoảng cách
            double distance = 0;
            decimal phiShip = 0;
            string distanceText = "";

            if (!string.IsNullOrWhiteSpace(customerAddress))
            {
                try
                {
                    distance = await CalculateDistance(StoreAddress, customerAddress);
                    phiShip = CalculateShippingFee(tamTinh, distance);
                    distanceText = distance > 0 ? $"{distance:F1} km" : "Không xác định được khoảng cách";
                }

                catch
                {
                    phiShip = 15000;
                    distanceText = "Không thể tính khoảng cách";
                }
            }

            decimal tongTien = tamTinh + phiShip;

            ViewBag.TamTinh = tamTinh;
            ViewBag.TongTien = tongTien;
            ViewBag.Distance = distanceText;
            ViewBag.CustomerAddress = customerAddress;
            return View(chiTiet);
        }

        // Tính phí ship bằng AJAX
        [HttpGet]
        public async Task<JsonResult> CalculateShippingFeeAjax(string customerAddress)
        {
            if (Session["UserID"] == null)
                return Json(new { error = "Chưa đăng nhập" }, JsonRequestBehavior.AllowGet);

            if (string.IsNullOrWhiteSpace(customerAddress))
                return Json(new { error = "Địa chỉ không hợp lệ" }, JsonRequestBehavior.AllowGet);

            double distance = 0;

            try
            {
                int userId = (int)Session["UserID"];
                var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == userId);
                if (gioHang == null)
                    return Json(new { error = "Giỏ hàng trống" }, JsonRequestBehavior.AllowGet);

                var chiTiet = db.ChiTietGioHangs.Where(c => c.MaGioHang == gioHang.MaGioHang).ToList();
                decimal tamTinh = chiTiet.Sum(c => c.SanPham.GiaSP * c.SoLuong);

                distance = await CalculateDistance(StoreAddress, customerAddress);
                decimal phiShip = CalculateShippingFee(tamTinh, distance);
                decimal tongTien = tamTinh + phiShip;

                return Json(new
                {
                    success = true,
                    tamTinh,
                    phiShip,
                    tongTien,
                    distance,
                    distanceText = distance > 0 ? $"{distance:F1} km" : "Không xác định được khoảng cách",
                    isFreeShipping = phiShip == 0
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string message = distance <= 0
                    ? "Không xác định được khoảng cách. Vui lòng kiểm tra lại địa chỉ."
                    : "Lỗi khi tính phí vận chuyển.";

                return Json(new { error = message, details = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // API tính khoảng cách giữa 2 địa chỉ
        private async Task<double> CalculateDistance(string origin, string destination)
        {
            var originCoords = await GetCoordinates(origin);
            var destinationCoords = await GetCoordinates(destination);

            if (originCoords == null || destinationCoords == null) return 0;

            string url = $"https://api.mapbox.com/directions/v5/mapbox/driving/{originCoords.Item2},{originCoords.Item1};{destinationCoords.Item2},{destinationCoords.Item1}?access_token={MapboxToken}&overview=false";

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) return 0;

                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                var distance = json["routes"]?[0]?["distance"]?.Value<double>() ?? 0;
                return distance / 1000.0;
            }
        }

        // API lấy tọa độ từ địa chỉ (Mapbox)
        private async Task<Tuple<double, double>> GetCoordinates(string address)
        {
            string url = $"https://api.mapbox.com/geocoding/v5/mapbox.places/{Uri.EscapeDataString(address)}.json?access_token={MapboxToken}&limit=1";

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                try
                {
                    var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                    var coords = json["features"]?[0]?["center"] as JArray;
                    if (coords == null || coords.Count < 2) return null;

                    return Tuple.Create(coords[1].Value<double>(), coords[0].Value<double>());
                }
                catch
                {
                    return null;
                }
            }
        }

        // Tính phí vận chuyển dựa vào khoảng cách
        private decimal CalculateShippingFee(decimal subtotal, double distance)
        {
            if (subtotal >= 200000) return 0;
            if (distance <= 0) return 15000;
            if (distance <= 3) return 10000;
            if (distance <= 6) return 15000;
            if (distance <= 10) return 20000;
            return 25000;
        }
        [HttpPost]
        public async Task<JsonResult> PlaceOrder(string customerName, string customerAddress, string customerPhone, string notes, string paymentMethod)
        {
            if (Session["UserID"] == null)
                return Json(new { success = false, redirect = Url.Action("Index", "LoginSignUp") });

            if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(customerAddress)
                || string.IsNullOrWhiteSpace(customerPhone))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin." });
            }

            int userId = (int)Session["UserID"];
            var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == userId);
            if (gioHang == null)
            {
                return Json(new { success = false, message = "Giỏ hàng trống." });
            }

            var chiTietGioHang = db.ChiTietGioHangs.Where(c => c.MaGioHang == gioHang.MaGioHang).ToList();
            if (!chiTietGioHang.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng không có sản phẩm." });
            }

            decimal tamTinh = 0;
            foreach (var item in chiTietGioHang)
            {
                tamTinh += item.SoLuong * item.SanPham.GiaSP;
            }

            double distance = await CalculateDistance(StoreAddress, customerAddress);
            decimal phiShip = CalculateShippingFee(tamTinh, distance);

            decimal tongTien = tamTinh + phiShip;

            var donHang = new DonHang
            {
                MaKH = userId,
                NgayTao = DateTime.Now,
                TrangThai = "Chờ xử lý",
                PhuongThucThanhToan = paymentMethod,
                NgayThanhToan = null,
                Note = $"Ghi chú: {notes}\n",
                TongTien = tongTien,
                DiaChiGiao = customerAddress,
                NguoiNhan = customerName,
                SDTNguoiNhan = customerPhone,
                PhiVanChuyen = (int?)phiShip

            };

            db.DonHangs.Add(donHang);
            db.SaveChanges();

            foreach (var item in chiTietGioHang)
            {
                var chiTietDonHang = new ChiTietDonHang
                {
                    MaKH = userId,
                    MaDH = donHang.MaDH,
                    MaSP = item.MaSP,
                    SoLuong = item.SoLuong,
                    GiaSP = item.SanPham.GiaSP,
                    TongTien = item.SoLuong * item.SanPham.GiaSP,
                    Ngaygiao = null,
                    TenSP = item.SanPham.TenSP,
                };
                db.ChiTietDonHangs.Add(chiTietDonHang);
            }

            db.ChiTietGioHangs.RemoveRange(chiTietGioHang);
            db.GioHangs.Remove(gioHang);
            db.SaveChanges();

            if (paymentMethod == "COD")
            {

                return Json(new { success = true, message = "Đặt hàng thành công! Vui lòng chuẩn bị tiền khi nhận hàng." });
            }

            else if (paymentMethod == "PayPal")
            {
                // Trả về một chỉ dẫn để frontend redirect sang trang PayPal
                return Json(new { success = true, redirectToPaypal = true });
            }
            else
            {
                return Json(new { success = false, message = "Phương thức thanh toán không hợp lệ." });
            }
        }

        public ActionResult PaymentWithPaypal(string Cancel = null)
        {
            //getting the apiContext  
            APIContext apiContext = PaypalConfiguration.GetAPIContext();
            try
            {
                //A resource representing a Payer that funds a payment Payment Method as paypal  
                //Payer Id will be returned when payment proceeds or click to pay  
                string payerId = Request.Params["PayerID"];
                if (string.IsNullOrEmpty(payerId))
                {
                    //this section will be executed first because PayerID doesn't exist  
                    //it is returned by the create function call of the payment class  
                    // Creating a payment  
                    // baseURL is the url on which paypal sendsback the data.  
                    string baseURI = Request.Url.Scheme + "://" + Request.Url.Authority + "/CheckOut/PaymentWithPayPal?";
                    //here we are generating guid for storing the paymentID received in session  
                    //which will be used in the payment execution  
                    var guid = Convert.ToString((new Random()).Next(100000));
                    //CreatePayment function gives us the payment approval url  
                    //on which payer is redirected for paypal account payment  
                    var createdPayment = this.CreatePayment(apiContext, baseURI + "guid=" + guid);
                    //get links returned from paypal in response to Create function call  
                    var links = createdPayment.links.GetEnumerator();
                    string paypalRedirectUrl = null;
                    while (links.MoveNext())
                    {
                        Links lnk = links.Current;
                        if (lnk.rel.ToLower().Trim().Equals("approval_url"))
                        {
                            //saving the payapalredirect URL to which user will be redirected for payment  
                            paypalRedirectUrl = lnk.href;
                        }
                    }
                    // saving the paymentID in the key guid  
                    Session.Add(guid, createdPayment.id);
                    return Redirect(paypalRedirectUrl);
                }
                else
                {
                    // This function exectues after receving all parameters for the payment  
                    var guid = Request.Params["guid"];
                    var executedPayment = ExecutePayment(apiContext, payerId, Session[guid] as string);
                    //If executed payment failed then we will show payment failure message to user  
                    if (executedPayment.state.ToLower() != "approved")
                    {
                        return View("FailureView");
                    }
                }
            }
            catch (Exception ex)
            {
                return View("FailureView");
            }
            //on successful payment, show success page to user.  
            return RedirectToAction("Index", "Home");
        }
        private PayPal.Api.Payment payment;
        private Payment ExecutePayment(APIContext apiContext, string payerId, string paymentId)
        {
            var paymentExecution = new PaymentExecution()
            {
                payer_id = payerId
            };
            this.payment = new Payment()
            {
                id = paymentId
            };
            return this.payment.Execute(apiContext, paymentExecution);
        }
        private Payment CreatePayment(APIContext apiContext, string redirectUrl)
        {
            // Lấy userId hiện tại (giả sử bạn lưu trong Session)
            int userId = (int)Session["UserID"];

            // Lấy giỏ hàng của user trong DB
            var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == userId);
            if (gioHang == null)
            {
                throw new Exception("Giỏ hàng không tồn tại");
            }

            // Lấy chi tiết giỏ hàng (sản phẩm và số lượng)
            var chiTietGioHang = db.ChiTietGioHangs.Where(c => c.MaGioHang == gioHang.MaGioHang).ToList();

            var itemList = new ItemList()
            {
                items = new List<Item>()
            };

            decimal subtotalVND = 0;
            const decimal tyGiaUSD = 24000; // giả sử 1 USD = 24,000 VND

            foreach (var item in chiTietGioHang)
            {
                decimal giaVND = item.SanPham.GiaSP; // Giá VND của sản phẩm trong DB
                int soLuong = item.SoLuong;

                decimal tongTienVND = giaVND * soLuong;
                subtotalVND += tongTienVND;

                decimal giaUSD = Math.Round(giaVND / tyGiaUSD, 2);
                string priceUSD = giaUSD.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

                itemList.items.Add(new Item()
                {
                    name = item.SanPham.TenSP,
                    currency = "USD",
                    price = priceUSD,
                    quantity = soLuong.ToString(),
                    sku = "SP" + item.MaSP // hoặc dùng mã khác nếu có
                });
            }

            // Các phí khác (tax, shipping) giả sử cố định, cũng quy đổi sang USD
            decimal shippingVND = 15000;
            decimal taxVND = 5000;

            decimal shippingUSD = Math.Round(shippingVND / tyGiaUSD, 2);
            decimal taxUSD = Math.Round(taxVND / tyGiaUSD, 2);
            decimal subtotalUSD = Math.Round(subtotalVND / tyGiaUSD, 2);
            decimal totalUSD = subtotalUSD + taxUSD + shippingUSD;

            var details = new Details()
            {
                tax = taxUSD.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                shipping = shippingUSD.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                subtotal = subtotalUSD.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
            };

            var amount = new Amount()
            {
                currency = "USD",
                total = totalUSD.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                details = details
            };

            var transactionList = new List<Transaction>();

            var paypalOrderId = DateTime.Now.Ticks;

            transactionList.Add(new Transaction()
            {
                description = $"Invoice #{paypalOrderId}",
                invoice_number = paypalOrderId.ToString(),
                amount = amount,
                item_list = itemList
            });

            var payer = new Payer() { payment_method = "paypal" };

            var redirUrls = new RedirectUrls()
            {
                cancel_url = redirectUrl + "&Cancel=true",
                return_url = redirectUrl
            };

            this.payment = new Payment()
            {
                intent = "sale",
                payer = payer,
                transactions = transactionList,
                redirect_urls = redirUrls
            };

            return this.payment.Create(apiContext);
        }




        public ActionResult FailureView()
        {
            return View();
        }


    }
}