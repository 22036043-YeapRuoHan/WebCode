﻿using Microsoft.AspNetCore.Mvc;

namespace Lesson04.Controllers
{
    public class OrganicController : Controller
    {
        public IActionResult Products()
        {
            string sql =
               @"SELECT OrgCode AS CODE, 
                     OrgDesc AS DESCRIPTION, 
                     Price AS PRICE, 
                     Gram AS GRAMS, 
                     Country AS COUNTRY
                FROM OrgProduct";

            DataTable dt = DBUtl.GetTable(sql);

            return View(dt);
        }

        public IActionResult Index()
        {
            return View("Organic");
        }

        public IActionResult Subscription()
        {
            return View("Subscription");
        }

        public IActionResult Confirmation()
        {
            IFormCollection form = HttpContext.Request.Form;

            string name = form["Name"].ToString().Trim();
            string email = form["Email"].ToString().Trim();
            string refer = form["Refer"].ToString().Trim();
            string comments = form["Comments"].ToString().Trim();
            string gender = form["Gender"].ToString();

            // Read CheckBoxes  
            string recipe = form["Recipe"].ToString();
            string news = form["News"].ToString();
            string offers = form["Offers"].ToString();

            // Validate User Enters and Selects all Fields
            if (ValidUtl.CheckIfEmpty(name, email, refer, comments, gender))
            {
                ViewData["Message"] = "Please enter or select all fields";
                return View("Subscription");
            }

            // Get CheckBoxes for Interest
            string interest = "";
            if (recipe.Equals("Recipe"))
                interest += "Recipe, ";
            if (news.Equals("News"))
                interest += "News, ";
            if (offers.Equals("Offers"))
                interest += "Offers, ";
            if (!interest.Equals(""))
                interest = interest[0..^2];

            // Display View
            ViewData["name"] = name;
            ViewData["email"] = email;
            ViewData["gender"] = gender;
            ViewData["interest"] = interest;
            ViewData["refer"] = refer;
            ViewData["comments"] = comments;

            return View("Confirmation");
        }

    }
}
