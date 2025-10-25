using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace NhanViet.JobOrders.Controllers;

[Area("NhanViet.JobOrders")]
public sealed class HomeController : Controller
{
    public ActionResult Index()
    {
        return View();
    }
}
