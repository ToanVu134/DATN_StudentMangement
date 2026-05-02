using System;
using System.Web.Mvc;
using System.Web.Routing;

namespace DATN_StudentMangement.Controllers
{
    public class BaseController : Controller
    {
        // Hàm này chạy trước mọi Action của các Controller kế thừa nó
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Session["Quyen"] == null)
            {
                // Nếu chưa đăng nhập, chuyển hướng về trang DangNhap của HeThong
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "HeThong", action = "DangNhap" })
                );
            }
            base.OnActionExecuting(filterContext);
        }
    }
}