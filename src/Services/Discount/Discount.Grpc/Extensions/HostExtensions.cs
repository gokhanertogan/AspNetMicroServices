using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Discount.Grpc.Extensions
{
    public static class HostExtensions
    {
        public static IHost MigrateDatabase<TContext>(this IHost host, int? retry=0)
        {
            int retryForAvailability = retry.Value;

            using (var scope=host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var configuration = services.GetRequiredService<IConfiguration>();
                var logger = services.GetRequiredService<ILogger<TContext>>();

                try
                {
                    logger.LogInformation("Migrating postresql database.");

                    using var connection = new NpgsqlConnection(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));
                    connection.Open();

                    using var command = new NpgsqlCommand
                    {
                        Connection = connection
                    };

                    command.CommandText = "DROP TABLE IF EXISTS Coupon";
                    command.ExecuteNonQuery();

                    command.CommandText = @"Create Table Coupon(ID SERIAL PRIMARY KEY NOT NULL,	ProductName Varchar(24) not null,Description TEXT,Amount INT)";
                    command.ExecuteNonQuery();

                    command.CommandText = "insert into Coupon (ProductName, Description, Amount) Values('IPhone X','IPhone Discount',150)";
                    command.ExecuteNonQuery();

                    command.CommandText = "insert into Coupon (ProductName, Description, Amount) Values ('Samsung 10', 'Samsung Discount', 100)";
                    command.ExecuteNonQuery();

                    logger.LogInformation("Migrated postresql database.");

                }
                catch (NpgsqlException ex)
                {
                    logger.LogError(ex,"An error occurred while migrrating the postresql database");

                    if (retryForAvailability<50)
                    {
                        retryForAvailability++;
                        System.Threading.Thread.Sleep(2000);
                        MigrateDatabase<TContext>(host,retryForAvailability);
                    }
                }
            }

            return host;
        }
    }
}
