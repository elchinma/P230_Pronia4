using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using NuGet.ContentModel;
using P230_Pronia.DAL;
using P230_Pronia.Entities;
using System.Web.Http.ModelBinding;

namespace P230_Pronia.Areas.ProniaAdmin.Controllers
{
    [Area("ProniaAdmin")]
    public class SliderController : Controller
    {
        private readonly ProniaDbContext _context;

        public SliderController(ProniaDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            IEnumerable<Slider> sliders = _context.Sliders.AsEnumerable();
            return View(sliders);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        //[AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Create(Slider newSlider)
        {
            if (newSlider.Image == null)
            {
                ModelState.AddModelError("Image", "Please choose image");
                return View();
            }
            if (!newSlider.Image.ContentType.Contains("image/"))
            {
                ModelState.AddModelError("Image", "Please choose image type file");
                return View();
            }
            if ((double)newSlider.Image.Length / 1024 / 1024 > 1)
            {
                ModelState.AddModelError("Image", "Image size has to be maximum 1MB");
                return View();
            }
            var rootPath = @"C:\Users\Lenovo\source\repos\P230_Pronia\P230_Pronia\wwwroot";
            var folderPath = Path.Combine(rootPath, "assets", "images", "website-images");
            Random r = new();
            int random = r.Next(0, 1000);
            var fileName = string.Concat(random,newSlider.Image.FileName);
            var path = Path.Combine(folderPath, fileName);


            using (FileStream stream = new(path, FileMode.Create))
            {
                await newSlider.Image.CopyToAsync(stream);
            }
            newSlider.ImagePath = fileName;
            _context.Sliders.Add(newSlider);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }
    }
    [HttpPost]
    public async Task<IActionResult> EditEntityPost(string dbSetName, string id, [FromForm] object formData, ModelState modelState, object ViewBag)
    {
        var entityToEdit = GetEntityFromDbSet(dbSetName, id, out var dbContextObject, out var entityType, out var relationships);

        dbContextObject.Attach(entityToEdit);

        var databaseGeneratedProperties =
       entityToEdit.GetType().GetProperties()
       .Where(p => p.GetCustomAttributes().Any(a => a.GetType().Name.Contains("DatabaseGenerated"))).Select(p => p.Name);

        foreach (var fkProperty in entityToEdit.GetType().GetProperties()
        .Where(p => p.GetCustomAttributes().Any(a => a.GetType().Name.Contains("ForeignKey"))).Select(p => p.Name))
        {
            if (!ModelState.ContainsKey(fkProperty))
            {
                continue;
            }
            modelState[fkProperty].Errors.Clear();
        }

        if (ModelState.ValidationState == ModelValidationState.Valid)
        {
            await dbContextObject.SaveChangesAsync();
        }

        return View("Edit", entityToEdit);
    }

    private async Task AddByteArrayFiles(object entityToEdit)
    {
        object Request = null;
        foreach (var file in Request.Form.Files)
        {
            var matchingProperty = entityToEdit.GetType().GetProperties()
                .FirstOrDefault(prop => prop.Name == file.Name && prop.PropertyType == typeof(byte[]));
            if (matchingProperty != null)
            {
                var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                matchingProperty.SetValue(entityToEdit, memoryStream.ToArray());
            }
        }
    }


}

