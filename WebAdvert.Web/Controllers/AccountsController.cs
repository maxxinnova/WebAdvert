namespace WebAdvert.Web.Controllers
{
    using System.Threading.Tasks;
    using Amazon.AspNetCore.Identity.Cognito;
    using Amazon.Extensions.CognitoAuthentication;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Models;

    public class AccountsController : Controller
    {
        private readonly SignInManager<CognitoUser> _signInManager;
        private readonly UserManager<CognitoUser> _userManager;
        private readonly CognitoUserPool _userPool;

        public AccountsController(SignInManager<CognitoUser> signInManager, UserManager<CognitoUser> userManager,
            CognitoUserPool userPool)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userPool = userPool;
        }

        // GET: /<controller>/
        public async Task<IActionResult> Index()
        {
            return await Task.FromResult(View());
        }

        [HttpGet]
        public async Task<IActionResult> Signup()
        {
            return await Task.FromResult<IActionResult>(View(new SignupModel()));
        }

        [HttpPost]
        public async Task<IActionResult> Signup(SignupModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = _userPool.GetUser(model.Email);

            if (user.Status != null)
            {
                ModelState.AddModelError("User Exists", "User with this email already exists");
                return View(model);
            }

            user.Attributes.Add(CognitoAttribute.Name.AttributeName, model.Email);
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(error.Code, error.Description);
                }

                return View(model);
            }

            return RedirectToAction("Confirm");
        }

        [HttpGet]
        public async Task<IActionResult> Confirm()
        {
            return await Task.FromResult(View(new ConfirmModel()));
        }

        [HttpPost]
        public async Task<IActionResult> Confirm(ConfirmModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userPool.FindByIdAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("User Not Found", "A user with the given address was not found");
                return View(model);
            }

            var userManager = _userManager as CognitoUserManager<CognitoUser>;

            var result = await userManager.ConfirmSignUpAsync(user, model.Code, true);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(error.Code, error.Description);
                }

                return View(model);
            }

            return RedirectToAction(controllerName: "Home", actionName: "Index");
        }

        [HttpGet]
        public async Task<IActionResult> Login()
        {
            return await Task.FromResult(View(new LoginModel()));
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
            {   
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("Login Failure", "Email or password is incorrect");
                return View(model);
            }

            return RedirectToAction(controllerName: "Home", actionName: "Index");
        }
    }
}
