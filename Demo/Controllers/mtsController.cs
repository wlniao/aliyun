using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers
{
    public class mtsController : Controller
    {
        #region /提交转码
        public IActionResult submitjobs()
        {
            if (Request.Cookies.ContainsKey("Login") && Request.Cookies["Login"].ToString() == "true")
            {
                return View();
            }
            else
            {
                return View("Login");
            }
        }
        #endregion
    }
}
