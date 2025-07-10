using BlazorServerApp_Server;
using BlazorServerApp_Server.Components;
using BlazorServerApp_Server.Components.Pages;
using BlazorServerApp_Server.Data;
using BlazorServerApp_Server.Data.Model;
using BlazorServerApp_Server.Hubs.BlazorServerApp_Server.Hubs;
using BlazorServerApp_Server.Middleware;
using BlazorServerApp_Server.Redis;
using BlazorServerApp_Server.Services;
using BlazorServerApp_Server.Services.BlazorServerApp_Server.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<HtmlCache>();

builder.Services.AddSingleton<RedisHtmlCache>();
builder.Services.AddSingleton<RedisPrerenderRegistry>();

builder.Services.AddScoped<HtmlRenderer>();
//builder.Services.AddScoped<WeatherPrerenderService>();
builder.Services.AddHostedService<PrerenderService>();
builder.Services.AddSingleton<NavigationTracker>();
builder.Services.AddSingleton<NavigationRuleEngine>();
builder.Services.AddSingleton<NavigationPredictorService>();
builder.Services.AddScoped<BrowserHistoryService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddHttpContextAccessor();
// Program.cs
builder.Services.AddSingleton<InMemoryPageHistoryService>();

builder.Services.AddScoped<BackgroundPagePrerenderer>();
builder.Services.AddSingleton<WeatherPrerenderDataService>(); 
builder.Services.AddMemoryCache();
builder.Services.AddHostedService<SqlServiceBrokerListener>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<PrerenderRegistry>();
builder.Services.AddScoped<ReportDataService>();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// --- Configure Redis ---
// Get Redis connection string from appsettings.json
var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection");
if (string.IsNullOrEmpty(redisConnectionString))
{
    Console.WriteLine("Warning: RedisConnection string is not configured in appsettings.json. Redis cache will not be used.");
    // Fallback to in-memory if Redis not configured, or throw error based on preference.
    // For this example, we'll proceed assuming it's configured.
}

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    // Configure your Redis connection here
    // Example: "localhost:6379" or "yourredis.azure.com:6380,password=YOUR_PASSWORD,ssl=True,abortConnect=False"
    return ConnectionMultiplexer.Connect(redisConnectionString ?? "localhost:6379");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.UseMigrationsEndPoint();
    // Seed the database
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            context.Database.Migrate(); 
            await SeedData(context); 
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }

    app.UseExceptionHandler("/Weather2", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseNavigationHistory(); 

app.UseAntiforgery();
app.MapHub<WeatherHub>("/weatherhub");
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// --- Database Seeding Method ---
async Task SeedData(ApplicationDbContext context)
{
    if (!context.Products.Any())
    {
        Console.WriteLine("Seeding products...");

        var products = new List<Product>
        {
            new Product
            {
                
                Name = "Blazor Master Laptop Pro",
                Description = "Experience unparalleled performance with the Blazor Master Laptop Pro. Designed for developers and creative professionals, this laptop features a stunning 15-inch Retina display, an octa-core processor, and 32GB of RAM. Perfect for demanding tasks like 3D rendering, video editing, and, of course, building cutting-edge Blazor applications.",
                Price = 1899.99m,
                ImageUrl = "https://picsum.photos/id/237/200/300",
                AdditionalImageUrls = new List<string>
                {
                    "https://picsum.photos/id/237/200/150",
                    "https://picsum.photos/id/237/200/300",
                    "https://picsum.photos/id/237/200/300",
                    "https://picsum.photos/id/237/200/300"
                },
                Brand = "TechBlaze",
                Category = "Laptops",
                Rating = 4.8,
                ReviewsCount = 125,
                LongFeatureList = "<ul><li>**Supercharged Performance:** Latest generation Intel/AMD processor for lightning-fast computations.</li><li>**Vibrant Retina Display:** Immerse yourself in breathtaking visuals with true-to-life colors and incredible detail.</li><li>**All-Day Battery Life:** Work and play for longer with an optimized power management system.</li><li>**Thunderbolt 4 Ports:** Blazing-fast data transfer and versatile connectivity.</li><li>**Advanced Cooling System:** Keeps your system running cool under heavy loads.</li><li>**Premium Aluminum Unibody:** Sleek, durable, and lightweight design.</li><li>**High-Fidelity Audio:** Crystal-clear sound for entertainment and video calls.</li></ul>",
                TechnicalSpecifications = "**Processor:** Octa-Core Intel i9-14900K | **RAM:** 32GB DDR5 | **Storage:** 1TB NVMe SSD | **Graphics:** NVIDIA GeForce RTX 4080 | **Display:** 15.6-inch Retina XDR (3072x1920) | **Operating System:** Windows 11 Pro | **Ports:** 2x Thunderbolt 4, 2x USB-A 3.2, HDMI 2.1, Headphone Jack | **Weight:** 1.8 kg"
            },
            new Product
            {
                
                Name = "Quantum Keyboard RGB",
                Description = "Elevate your typing experience with the Quantum Keyboard RGB. Featuring tactile mechanical switches, customizable RGB backlighting, and a durable aluminum frame, this keyboard is perfect for gaming and productivity.",
                Price = 129.99m,
                ImageUrl = "https://picsum.photos/id/237/200/300",
                AdditionalImageUrls = new List<string>
                {
                    "https://picsum.photos/id/237/200/300",
                    "https://picsum.photos/id/237/200/300",
                    "https://picsum.photos/id/237/200/300"
                },
                Brand = "ErgoKeys",
                Category = "Peripherals",
                Rating = 4.5,
                ReviewsCount = 88,
                LongFeatureList = "<ul><li>**Mechanical Switches:** Choose from various switch types (Cherry MX Red, Blue, Brown) for your preferred feel.</li><li>**Per-Key RGB Lighting:** Customize every key with millions of colors and dynamic effects.</li><li>**Full N-Key Rollover:** Ensures every keystroke is registered, no matter how fast you type.</li><li>**Durable Aluminum Top Plate:** Built to withstand intense gaming sessions.</li><li>**Detachable USB-C Cable:** For easy portability.</li></ul>",
                TechnicalSpecifications = "**Connectivity:** USB-C to USB-A | **Switch Type:** Cherry MX Mechanical | **Backlighting:** Per-key RGB | **Layout:** Full-size (104 keys) | **Material:** Aluminum & ABS Plastic | **Dimensions:** 440 x 130 x 35 mm | **Weight:** 1.1 kg"
            },
            new Product
            {
                
                Name = "Aura Wireless Mouse",
                Description = "The Aura Wireless Mouse combines precision and comfort. With a high-DPI sensor and ergonomic design, it's ideal for long hours of work or intense gaming.",
                Price = 59.99m,
                ImageUrl = "https://picsum.photos/id/237/200/300",
                AdditionalImageUrls = new List<string>
                {
                    "https://picsum.photos/id/237/200/300",
                    "https://picsum.photos/id/237/200/300",
                    "https://picsum.photos/id/237/200/300"
                },
                Brand = "GlideTech",
                Category = "Peripherals",
                Rating = 4.7,
                ReviewsCount = 203,
                LongFeatureList = "<ul><li>**Ultra-Precise Sensor:** Up to 16,000 DPI for pixel-perfect tracking.</li><li>**Ergonomic Design:** Contoured shape fits comfortably in your hand.</li><li>**Wireless Freedom:** Lag-free 2.4GHz wireless connection.</li><li>**Programmable Buttons:** Customize functions for enhanced productivity or gaming.</li><li>**Long Battery Life:** Extended usage on a single charge.</li></ul>",
                TechnicalSpecifications = "**Connectivity:** 2.4GHz Wireless / USB | **Sensor:** Optical, up to 16000 DPI | **Buttons:** 6 programmable | **Battery Life:** Up to 70 hours | **Weight:** 95g | **Dimensions:** 125 x 65 x 40 mm"
            },
            new Product
            {
                
                Name = "Studio Pro Headphones",
                Description = "Immerse yourself in rich, detailed sound with the Studio Pro Headphones. Featuring noise-cancelling technology and plush earcups, they're perfect for audiophiles and professionals alike.",
                Price = 249.99m,
                ImageUrl = "https://picsum.photos/id/237/200/300",
                AdditionalImageUrls = new List<string>
                {
                    "https://picsum.photos/id/237/200/300",
                    "https://picsum.photos/id/237/200/300",
                    "https://picsum.photos/id/237/200/300"
                },
                Brand = "SoundSphere",
                Category = "Audio",
                Rating = 4.6,
                ReviewsCount = 91,
                LongFeatureList = "<ul><li>**Active Noise Cancellation:** Block out distractions for pure audio enjoyment.</li><li>**Hi-Res Audio Certified:** Experience music as the artist intended.</li><li>**Comfortable Over-Ear Design:** Plush earcups and adjustable headband for long listening sessions.</li><li>**Foldable Design:** Easy to carry and store.</li><li>**Built-in Microphone:** For clear calls and voice commands.</li></ul>",
                TechnicalSpecifications = "**Connectivity:** Bluetooth 5.0 / 3.5mm Jack | **Driver Size:** 40mm | **Frequency Response:** 20Hz - 20kHz | **Battery Life:** Up to 30 hours (ANC on) | **Weight:** 280g | **Features:** Active Noise Cancellation, Hi-Res Audio"
            },
            new Product
            {
                
                Name = "Ultra-Wide Monitor 4K",
                Description = "Expand your visual workspace with the Ultra-Wide Monitor 4K. Perfect for multitasking, gaming, and immersive media consumption.",
                Price = 699.99m,
                ImageUrl = "https://picsum.photos/id/237/200/300",
                AdditionalImageUrls = new List<string>
                {
                    "https://picsum.photos/id/237/200/300",
                    "https://picsum.photos/id/237/200/300",
                    "https://picsum.photos/id/237/200/300"
                },
                Brand = "ViewMax",
                Category = "Displays",
                Rating = 4.7,
                ReviewsCount = 65,
                LongFeatureList = "<ul><li>**Stunning 4K Resolution:** Incredible clarity and detail for all your content.</li><li>**Ultrawide Aspect Ratio:** More screen real estate for enhanced productivity.</li><li>**HDR Support:** Vibrant colors and deep contrast for a lifelike experience.</li><li>**Multiple Connectivity Options:** HDMI, DisplayPort, and USB-C.</li><li>**Adjustable Stand:** Tilt, swivel, and height adjustments for ergonomic comfort.</li></ul>",
                TechnicalSpecifications = "**Panel Type:** IPS | **Resolution:** 3840x1600 (UWQHD+) | **Refresh Rate:** 75Hz | **Response Time:** 5ms | **Brightness:** 300 nits | **Contrast Ratio:** 1000:1 | **Ports:** 2x HDMI 2.0, 1x DisplayPort 1.4, 1x USB-C | **Features:** HDR10, FreeSync"
            },
            new Product
            {
                
                Name = "Smart Home Hub Gen 3",
                Description = "Control your entire smart home with the Smart Home Hub Gen 3. Connect all your devices for seamless automation and voice control.",
                Price = 99.99m,
                ImageUrl = "https://via.placeholder.com/600x400?text=Smart+Home+Hub",
                AdditionalImageUrls = new List<string>
                {
                    "https://via.placeholder.com/200x150?text=Hub+Lights",
                    "https://via.placeholder.com/200x150?text=Hub+Sensors",
                    "https://via.placeholder.com/200x150?text=Hub+Voice"
                },
                Brand = "HomeConnect",
                Category = "Smart Home",
                Rating = 4.3,
                ReviewsCount = 78,
                LongFeatureList = "<ul><li>**Universal Compatibility:** Works with hundreds of smart devices across various brands.</li><li>**Voice Assistant Integration:** Control devices with Amazon Alexa or Google Assistant.</li><li>**Advanced Automation:** Create custom routines and schedules.</li><li>**Local Processing:** Faster response times and enhanced privacy.</li><li>**Sleek, Compact Design:** Blends seamlessly into any home decor.</li></ul>",
                TechnicalSpecifications = "**Connectivity:** Wi-Fi, Zigbee, Z-Wave, Bluetooth | **Voice Assistants:** Alexa, Google Assistant | **Power:** DC 5V | **Dimensions:** 100 x 100 x 25 mm | **Weight:** 150g"
            }
        };
        foreach (var product in products)
        {
            var productAdded = await context.Products.AddAsync(product);
        }
        await context.SaveChangesAsync();

        // Seed related products after products are saved to get valid IDs
        await context.ProductRelatedProducts.AddRangeAsync(
            new ProductRelatedProduct { ProductId = 1, RelatedProductId = 2 }, // Laptop related to keyboard
            new ProductRelatedProduct { ProductId = 1, RelatedProductId = 3 }, // Laptop related to mouse
            new ProductRelatedProduct { ProductId = 1, RelatedProductId = 5 }, // Laptop related to monitor
            new ProductRelatedProduct { ProductId = 2, RelatedProductId = 3 }, // Keyboard related to mouse
            new ProductRelatedProduct { ProductId = 3, RelatedProductId = 2 }, // Mouse related to keyboard
            new ProductRelatedProduct { ProductId = 4, RelatedProductId = 1 }, // Headphones related to laptop
            new ProductRelatedProduct { ProductId = 5, RelatedProductId = 1 }, // Monitor related to laptop
            new ProductRelatedProduct { ProductId = 5, RelatedProductId = 4 }  // Monitor related to headphones
        );
        await context.SaveChangesAsync();
        Console.WriteLine("Products and related products seeded successfully!");
    }
    else
    {
        Console.WriteLine("Database already contains products. Skipping seeding.");
    }
}
