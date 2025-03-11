using Mavercloud.PDF.ElementHandler;
using Mavercloud.PDF.Biz.Api.Models;
using Mavercloud.PDF.Biz.Entries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mavercloud.PDF.Biz.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PDFController : ApiControllerBase
    {
        private readonly IWebHostEnvironment hostingEnvironment;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostingEnvironment"></param>
        public PDFController(IWebHostEnvironment hostingEnvironment)
        {
            this.hostingEnvironment = hostingEnvironment;
        }

        [HttpPost("[action]")]
        public ApiModel Create([FromBody] BizPdfCreatorParameters parameters)
        {
            var apiModel = new ApiModel();
            try
            {
                var fontDirectory = Path.Combine(hostingEnvironment.ContentRootPath, "AppResources/Fonts");

                PDFFontHelper.RegisterDirectory(fontDirectory, parameters.FontEncoding);
                SpecialCharHelper.Initialize(parameters.SpecialCharConfigXml);
                ElementGenerator.SetDocumentMargins(parameters.Margins);

                if (string.IsNullOrEmpty(parameters.FileOutputDir))
                {
                    parameters.FileOutputDir = Path.Combine(hostingEnvironment.ContentRootPath, "TempStore");
                }
                if (!Directory.Exists(parameters.FileOutputDir))
                {
                    Directory.CreateDirectory(parameters.FileOutputDir);
                }


                var fullFilePath = string.Empty;
                using (var creator = new BizPdfCreator(parameters))
                {
                    fullFilePath = creator.CreateAndSave();
                }
                apiModel.Success = true;
                apiModel.Data = fullFilePath;
            }
            catch (Exception ex)
            {
                apiModel.Success = false;
                apiModel.ErrorMessage = ex.Message;
            }
            finally
            {
                if (parameters != null && !parameters.UserLocalDirectory && !string.IsNullOrEmpty(parameters.FileOutputDir))
                {
                    try
                    {
                        Directory.Delete(parameters.FileOutputDir, true);
                    }
                    catch { }
                }
            }
            return apiModel;
        }
    }
}
