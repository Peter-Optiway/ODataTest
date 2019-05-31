using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ODataTest.Controllers;

namespace ODataTest
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddMvc();
            services.AddOData();
            services.AddODataQueryFilter();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var modelBuilder = new ODataConventionModelBuilder(serviceProvider);
            var entitySet = modelBuilder.EntitySet<Book>(BookController.BookPrefix);
            var entityType = entitySet.EntityType;
            entityType.HasKey(x => x.ISBN);
            entityType.Collection.Function(nameof(BookController.Search)).ReturnsCollectionFromEntitySet<Book>(BookController.BookPrefix).Parameter<string>("prefix").Required();



            app.UseMvc(builder =>
            {
                builder.EnableDependencyInjection();
                builder.Expand().Select().Count().Filter().OrderBy().MaxTop(100);
                builder.MapODataServiceRoute("v1", "v1", modelBuilder.GetEdmModel());
            });
        }
    }

    public class Book
    {
        public string ISBN { get; set; }
        public string Name { get; set; }
    }
}


namespace ODataTest.Controllers
{
    [ODataRoutePrefix(BookController.BookPrefix)]
    public class BookController : ODataController
    {
        public const string BookPrefix = "Books";
        public List<Book> books = new List<Book>()
        {
            new Book { ISBN = "99921-58-10-7", Name = "Test1" },
            new Book { ISBN = "9971-5-0210-0", Name = "Test2" },
            new Book { ISBN = "960-425-059-0", Name = "Test3" },
            new Book { ISBN = "966-425-059-0", Name = "Test4" },
        };

        [ODataRoute("", RouteName = "v1")]
        [EnableQuery]
        public IQueryable<Book> Get() => books.AsQueryable();

        [ODataRoute("({isbn})", RouteName = "v1")]
        [EnableQuery]
        public SingleResult<Book> Get(string isbn) => SingleResult.Create(books.AsQueryable().Where(x => x.ISBN == isbn));

        [ODataRoute("Default." + nameof(Search) + "(Prefix={prefix})", RouteName = "v1")]
        [EnableQuery]
        public IQueryable<Book> Search(string prefix) => books.Where(x => x.ISBN.StartsWith(prefix)).AsQueryable();
    }
}
