﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Westwind.AspNetCore.LiveReload;

namespace LiveReloadServer
{
    public class Startup
    {

        private string WebRoot;
        private int Port = 0;
        public bool UseLiveReload = true;
        private bool UseRazor = true;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Get Configuration Settings
            var lrEnabled = Configuration["LiveReloadEnabled"];
            UseLiveReload = string.IsNullOrEmpty(lrEnabled) ||
                            !lrEnabled.Equals("false", StringComparison.InvariantCultureIgnoreCase);
            var razEnabled = Configuration["RazorEnabled"];
            UseRazor = string.IsNullOrEmpty(razEnabled) ||
                       !razEnabled.Equals("false", StringComparison.InvariantCultureIgnoreCase);
            

            WebRoot = Configuration["WebRoot"];
            if (string.IsNullOrEmpty(WebRoot))
                WebRoot = Environment.CurrentDirectory;
            else
                WebRoot = Path.GetFullPath(WebRoot,Environment.CurrentDirectory);

            if (UseLiveReload)
            {
                services.AddLiveReload(opt =>
                {
                    opt.FolderToMonitor = WebRoot;
                    opt.LiveReloadEnabled = UseLiveReload;
                });
            }

            if (UseRazor)
            {
                services.AddRazorPages(opt => { opt.RootDirectory = "/"; })
                    .AddRazorRuntimeCompilation(
                        opt =>
                        {
                            // This would be useful but it's READ-ONLY
                            // opt.AdditionalReferencePaths = Path.Combine(WebRoot,"bin");

                            opt.FileProviders.Add(new PhysicalFileProvider(WebRoot));
                        });
            }
        }

        

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            bool useSsl = true;
            var temp = Configuration["UseSsl"];
            if (temp == "False")
                useSsl = false;

            bool openBrowser = true;
            temp = Configuration["OpenBrowser"];
            if (temp == "False")
                openBrowser = true;

            string defaultFiles = Configuration["DefaultFiles"];
            if (string.IsNullOrEmpty(defaultFiles))
                defaultFiles = "index.html,default.htm,default.html";

            var strPort = Configuration["Port"];
            if (!int.TryParse(strPort, out Port))
                Port = 5000;
            
            env.ContentRootPath = WebRoot;
            env.WebRootPath = WebRoot;

            if (UseLiveReload)
            {
                app.UseLiveReload();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = new PhysicalFileProvider(WebRoot),
                DefaultFileNames = new List<string>(defaultFiles.Split(',',';'))
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(WebRoot),
                RequestPath = new PathString("")
            });

            if (UseRazor)
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => { endpoints.MapRazorPages(); });
            }

            var url = $"http{(useSsl ? "s" : "")}://localhost:{Port}";

            Console.WriteLine("----------------------------------------------");
            Console.WriteLine("Live Reload Server");
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine($"(c) West Wind Technologies, 2018-{DateTime.Now.Year}\r\n");
            Console.WriteLine($"Site Url   : {url}");
            Console.WriteLine($"Site Path  : {WebRoot}");
            Console.WriteLine($"Live Reload: {UseLiveReload}");
            Console.WriteLine($"Use Razor  : {UseRazor}");
            Console.WriteLine("\r\npress Ctrl-C or Ctrl-Break to exit...");
            Console.WriteLine("'LiveReloadServer --help' for start options...");
            Console.WriteLine("----------------------------------------------");
            
            if (openBrowser)
                OpenUrl(url);
        }

        public static void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") {CreateNoWindow = true});
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }

            }

        }
    }
}
