using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Lesson13.Controllers;

public class AccountController : Controller
{
    private const string LOGIN_SQL =
       @"SELECT * FROM SysUser 
            WHERE UserId = '{0}' 
              AND UserPw = HASHBYTES('SHA1', '{1}')";

    private const string LASTLOGIN_SQL =
       @"UPDATE SysUser SET LastLogin=GETDATE() WHERE UserId='{0}'";

    private const string ROLE_COL = "UserRole";
    private const string NAME_COL = "FullName";

    private const string REDIRECT_CNTR = "Movie";
    private const string REDIRECT_ACTN = "ListMovies";

    private const string LOGIN_VIEW = "UserLogin";

    [AllowAnonymous]
    public IActionResult Login(string returnUrl = null!)
    {
        TempData["ReturnUrl"] = returnUrl;
        return View(LOGIN_VIEW);
    }

    [AllowAnonymous]
    [HttpPost]
    public IActionResult Login(UserLogin user)
    {
        if (!AuthenticateUser(user.UserID, user.Password, out ClaimsPrincipal principal))
        {
            ViewData["Message"] = "Incorrect User ID or Password";
            ViewData["MsgType"] = "warning";
            return View(LOGIN_VIEW);
        }
        else
        {
            HttpContext.SignInAsync(
               CookieAuthenticationDefaults.AuthenticationScheme,
               principal,
           new AuthenticationProperties
           {
               IsPersistent = user.RememberMe
           });

            // Update the Last Login Timestamp of the User
            DBUtl.ExecSQL(LASTLOGIN_SQL, user.UserID);

            if (TempData["returnUrl"] != null)
            {
                string returnUrl = TempData["returnUrl"]!.ToString()!;
                if (Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
            }

            return RedirectToAction(REDIRECT_ACTN, REDIRECT_CNTR);
        }
    }

    [Authorize]
    public IActionResult Logoff(string returnUrl = null!)
    {
        HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(REDIRECT_ACTN, REDIRECT_CNTR);
    }

    [AllowAnonymous]
    public IActionResult Forbidden()
    {
        return View();
    }

    [Authorize(Roles = "manager")]
    public IActionResult Users()
    {
        List<SysUser> list = DBUtl.GetList<SysUser>("SELECT * FROM SysUser WHERE UserRole='member' ");
        return View(list);
    }

    [Authorize(Roles = "manager")]
    public IActionResult Delete(string id)
    {
        string delete = "DELETE FROM SysUser WHERE UserId='{0}'";
        int res = DBUtl.ExecSQL(delete, id);
        if (res == 1)
        {
            TempData["Message"] = "User Record Deleted";
            TempData["MsgType"] = "success";
        }
        else
        {
            TempData["Message"] = DBUtl.DB_Message;
            TempData["MsgType"] = "danger";
        }

        return RedirectToAction("Users");
    }

    [AllowAnonymous]
    public IActionResult Register()
    {
        return View("UserRegister");
    }

    [AllowAnonymous]
    [HttpPost]
    public IActionResult Register(SysUser usr)
    {
        ModelState.Remove("UserRole"); // All new registrations will be initially members
        if (!ModelState.IsValid)
        {
            return View("UserRegister", usr);
        }
        else
        {
            string insert =
               @"INSERT INTO SysUser(UserId, UserPw, FullName, Email, UserRole) VALUES
                 ('{0}', HASHBYTES('SHA1', '{1}'), '{2}', '{3}', 'member')";
            if (DBUtl.ExecSQL(insert, usr.UserId, usr.UserPw, usr.FullName, usr.Email) == 1)
            {
                string template = @"Hi {0},<br/><br/>
                               Welcome to Movie Buff!
                               Your userid is <b>{1}</b> and password is <b>{2}</b>.
                               <br/><br/>Manager";
                string title = "Registration Successul - Welcome";
                string message = String.Format(template, usr.FullName, usr.UserId, usr.UserPw);
                if (EmailUtl.SendEmail(usr.Email, title, message, out string result))
                {
                    ViewData["Message"] = "User successfully registered";
                    ViewData["MsgType"] = "success";
                }
                else
                {
                    ViewData["Message"] = "User created, but email failed with message: " + result;
                    ViewData["MsgType"] = "warning";
                }
            }
            else
            {
                ViewData["Message"] = DBUtl.DB_Message;
                ViewData["MsgType"] = "danger";
            }
            return View("UserRegister");
        }
    }

    [AllowAnonymous]
    public IActionResult VerifyUserID(string userId)
    {
        string select = $"SELECT * FROM SysUser WHERE Userid='{userId}'";
        if (DBUtl.GetTable(select).Rows.Count > 0)
        {
            return Json($"[{userId}] already in use");
        }
        return Json(true);
    }

    private static bool AuthenticateUser(string uid, string pw, out ClaimsPrincipal principal)
    {
        principal = null!;

        DataTable ds = DBUtl.GetTable(LOGIN_SQL, uid, pw);
        if (ds.Rows.Count == 1)
        {
            principal =
               new ClaimsPrincipal(
                  new ClaimsIdentity(
                     new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, uid),
                    new Claim(ClaimTypes.Name, ds.Rows[0][NAME_COL]!.ToString()!),
                    new Claim(ClaimTypes.Role, ds.Rows[0][ROLE_COL]!.ToString()!)
                     }, "Basic"
                  )
               );
            return true;
        }
        return false;
    }

}