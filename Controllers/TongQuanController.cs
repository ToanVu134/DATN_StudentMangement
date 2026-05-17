using System.Linq;
using System.Web.Mvc;
using DATN_StudentMangement.Models;

namespace DATN_StudentMangement.Controllers
{
    public class TongQuanController : Controller
    {
        private QuanLiSinhVien_SenikaaEntities db = new QuanLiSinhVien_SenikaaEntities();

        public ActionResult Index()
        {
            if (Session["Quyen"] == null) return RedirectToAction("DangNhap", "HeThong");

            var thongKeTrangThai = db.Sinhviens
                .GroupBy(s => s.TrangThaiHocTap)
                .Select(g => new { Ten = g.Key, SoLuong = g.Count() })
                .ToList();

            ViewBag.LabelsTrangThai = thongKeTrangThai.Select(x => string.IsNullOrEmpty(x.Ten) ? "Khác" : x.Ten).ToArray();
            ViewBag.DataTrangThai = thongKeTrangThai.Select(x => x.SoLuong).ToArray();

            var thongKeNganh = db.Sinhviens
                .GroupBy(s => s.NganhHoc)
                .Select(g => new { Ten = g.Key, SoLuong = g.Count() })
                .ToList();

            ViewBag.LabelsNganh = thongKeNganh.Select(x => string.IsNullOrEmpty(x.Ten) ? "Khác" : x.Ten).ToArray();
            ViewBag.DataNganh = thongKeNganh.Select(x => x.SoLuong).ToArray();

            var thongKeKhoa = db.Sinhviens
                .GroupBy(s => s.KhoaNhapHoc)
                .Select(g => new { Ten = g.Key, SoLuong = g.Count() })
                .OrderBy(x => x.Ten)
                .ToList();

            ViewBag.LabelsKhoa = thongKeKhoa.Select(x => string.IsNullOrEmpty(x.Ten) ? "Khác" : x.Ten).ToArray();
            ViewBag.DataKhoa = thongKeKhoa.Select(x => x.SoLuong).ToArray();

            ViewBag.DSNganh = db.Sinhviens.Select(s => s.NganhHoc).Distinct().Where(n => !string.IsNullOrEmpty(n)).ToList();

            ViewBag.TongSinhVien = db.Sinhviens.Count();
            ViewBag.TongGiangVien = db.GiangViens.Count();
            ViewBag.TongLopHP = db.LopHPs.Count(l => l.TrangThai != "Đã khóa");

            return View();
        }

        [HttpGet]
        public JsonResult GetThongKeKhoa(string nganh)
        {
            var query = db.Sinhviens.AsQueryable();
            if (!string.IsNullOrEmpty(nganh))
            {
                query = query.Where(s => s.NganhHoc == nganh);
            }

            var thongKeKhoa = query
                .GroupBy(s => s.KhoaNhapHoc)
                .Select(g => new { Ten = g.Key, SoLuong = g.Count() })
                .OrderBy(x => x.Ten)
                .ToList();

            var labels = thongKeKhoa.Select(x => string.IsNullOrEmpty(x.Ten) ? "Khác" : x.Ten).ToArray();
            var data = thongKeKhoa.Select(x => x.SoLuong).ToArray();

            return Json(new { labels = labels, data = data }, JsonRequestBehavior.AllowGet);
        }
    }
}