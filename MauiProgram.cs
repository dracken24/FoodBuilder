using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using FoodBuilder.Config;
using FoodBuilder.Services;
using System;

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

			// Enregistrement des services Firebase/Firestore
			builder.Services.AddSingleton(new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(30)
			});
			builder.Services.AddSingleton(provider =>
			{
				HttpClient http = provider.GetRequiredService<HttpClient>();
				return new FirebaseAuthService(http, FirebaseConfig.ApiKey);
			});
			builder.Services.AddSingleton(provider =>
			{
				HttpClient http = provider.GetRequiredService<HttpClient>();
				FirebaseAuthService auth = provider.GetRequiredService<FirebaseAuthService>();
				return new FirestoreService(http, FirebaseConfig.ProjectId, auth);
			});

#if DEBUG
			builder.Logging.AddDebug();
#endif

			return builder.Build();
		}
	}
}
