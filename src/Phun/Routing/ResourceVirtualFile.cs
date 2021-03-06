﻿namespace Phun.Routing
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Reflection;
    using System.Web;
    using System.Web.Hosting;

    using Phun.Configuration;

    /// <summary>
    /// Get virtual file from resource.  Also use as a utility class.
    /// </summary>
    public class ResourceVirtualFile : VirtualFile
    {
        /// <summary>
        /// The virtual file path, this is use for debugging purposes.
        /// </summary>
        private readonly string virtualFilePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceVirtualFile"/> class.
        /// </summary>
        protected internal ResourceVirtualFile()
            : base("/UnitTestOnly")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceVirtualFile" /> class.
        /// </summary>
        /// <param name="virtualPath">The virtual path to the resource represented by this instance.</param>
        public ResourceVirtualFile(string virtualPath)
            : base(virtualPath)
        {
            this.Config = Bootstrapper.Default.Config;
            this.virtualFilePath = virtualPath;
        }

        /// <summary>
        /// Gets or sets the config.
        /// </summary>
        /// <value>
        /// The config.
        /// </value>
        protected internal ICmsConfiguration Config { get; set; }

        /// <summary>
        /// When overridden in a derived class, returns a read-only stream to the virtual resource.
        /// </summary>
        /// <returns>
        /// A read-only stream to the virtual file.
        /// </returns>
        public override System.IO.Stream Open()
        {
            var resourcePath = this.TranslateToResourcePath(this.virtualFilePath);
            var result = Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Concat("Phun.Properties.", resourcePath));
            if (resourcePath.EndsWith("scripts.phuncms.config.js", StringComparison.OrdinalIgnoreCase))
            {
                var fileString =
                    string.Format(ResourcePathUtility.ScriptsphuncmsConfigJs, this.Config.ResourceRouteNormalized, this.Config.ContentRouteNormalized)
                        .Replace("[", "{")
                        .Replace("]", "}");

                result = new MemoryStream(System.Text.Encoding.ASCII.GetBytes(fileString));
            }
            else if (result == null)
            {
                // try to get resource in all lowered case
                result = Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream(
                            string.Concat("Phun.Properties.", resourcePath.ToLowerInvariant()));
            }

            if (result == null)
            {
                throw new HttpException(404, "Resource virtual file not found: " + resourcePath);
            }

            return result;
        }


        /// <summary>
        /// Writes the file.
        /// </summary>
        /// <param name="context">The context.</param>
        public virtual void WriteFile(HttpContextBase context)
        {
            var response = context.Response;
            var stream = this.Open();

            if (stream == null)
            {
                throw new HttpException(404, "Path '" + this.virtualFilePath + "' cannot be found.");
            }
            
            // since this is embedded resource, set last modified by specific date
            if (!this.TrySet304(context))
            {
                response.ContentType = MimeTypes.GetContentType(System.IO.Path.GetExtension(this.virtualFilePath));
                stream.CopyTo(response.OutputStream);
                response.OutputStream.Flush();
            }

            response.End();
        }

        /// <summary>
        /// Tries the set304.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>To to set 304 response.</returns>
        public virtual bool TrySet304(HttpContextBase context, double hours = 24)
        {
            if (this.Config.DisableResourceCache
                || context.Request.Path.ToLowerInvariant().Contains("phuncms.config.js")
                || context.Request.QueryString.Count > 0)
            {
                context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
                context.Response.Cache.SetExpires(DateTime.MinValue);
                return false;
            }

            var currentDate = System.IO.File.GetCreationTime(Assembly.GetExecutingAssembly().Location);
            context.Response.Cache.SetLastModified(currentDate);
            context.Response.Cache.SetCacheability(HttpCacheability.Public);
            context.Response.Cache.SetExpires(DateTime.Now.AddHours(hours));
 
            DateTime previousDate;
            string data = context.Request.Headers["If-Modified-Since"] + string.Empty;
            if (DateTime.TryParse(data, out previousDate))
            {
                if (currentDate > previousDate.AddMilliseconds(100))
                 {
                     context.Response.StatusCode = 304;
                     context.Response.StatusDescription = "Not Modified";
                     return true;
                 }
            }

            return false;
        }

        /// <summary>
        /// Translates to resource path.
        /// </summary>
        /// <param name="virtualPath">The virtual path.</param>
        /// <returns>The resource path.</returns>
        protected string TranslateToResourcePath(string virtualPath)
        {
            // actual resource folders are translated to periods
            var resource = (virtualPath + string.Empty)
                .Replace("~", string.Empty)
                .Substring(this.Config.ResourceRouteNormalized.Length + 2)
                .Replace("/", ".")
                .Trim('.');

            return resource;
        }
    }
}
