using Microsoft.Extensions.Logging;
using Supabase;

namespace FoodBuilder
{
	public static class MauiProgram
	{
		public static MauiApp CreateMauiApp()
		{
			MauiAppBuilder builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
					fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				});
				
			// Configuration de Supabase
			string? url = Environment.GetEnvironmentVariable("SUPABASE_URL");
			string? key = Environment.GetEnvironmentVariable("SUPABASE_KEY");
			SupabaseOptions options = new SupabaseOptions
			{
				AutoRefreshToken = true,
				AutoConnectRealtime = true,
				// SessionHandler = new SupabaseSessionHandler() <-- This must be implemented by the developer
			};
			// Note the creation as a singleton.
			builder.Services.AddSingleton(provider => new Supabase.Client(url, key, options));

#if DEBUG
			builder.Logging.AddDebug();
#endif

			return builder.Build();
		}
	}
}
