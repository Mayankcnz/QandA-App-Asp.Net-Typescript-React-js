using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DbUp;
using MyQnA_APP.Data;

namespace MyQnA_APP
{
    /**
     * The code in this file executes when asp.net core app runs app.
     */
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

            // This gets the database connection from the appsettings.json file
            // and creates the database if it doesn't exist
            var connectionString =
                Configuration.GetConnectionString("DefaultConnection");
            EnsureDatabase.For.SqlDatabase(connectionString);

            /**
             * we have told DbUp where the database is and to look for SQL Scripts
             * that have been embedded in our project. We have also told DbUp to do the dabase
             * migrations in a transaction
             */
            var upgrader = DeployChanges.To.SqlDatabase(connectionString, null)
                .WithScriptsEmbeddedInAssembly(
                System.Reflection.Assembly.GetExecutingAssembly()
                )
                .WithTransaction()
                .LogToConsole()
                .Build();

            // The final step is to get DbUp to do a databse migration if there are any 
            // pending SQL Scripts
            if(upgrader.IsUpgradeRequired())
            {
                upgrader.PerformUpgrade();
            }

            services.AddControllers();

            // make data repository available for dependency injection
            // This will tell asp.net that whenever IDataRepositroy is referenced
            // in a constructor substitute an instance of the DataRepositopry class
            /**
             * The AddScoped method means only one instance of the DataRepository class
             * is created in the same Http request. This means the lifetime the lifetime of the class
             * that is created lassts for whole http request. 
             */
            services.AddScoped<IDataRepository, DataRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }else
            {

                app.UseHttpsRedirection();

            }


            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
