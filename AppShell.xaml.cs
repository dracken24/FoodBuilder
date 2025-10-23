using Microsoft.Maui.Controls;

namespace FoodBuilder
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            try
            {
                InitializeComponent();

                // Enregistrer les routes
                Routing.RegisterRoute("home", typeof(Pages.Home));
                Routing.RegisterRoute("favorites", typeof(Pages.Favorites));
                Routing.RegisterRoute("mealPlan", typeof(Pages.MealPlan));
                Routing.RegisterRoute("settings", typeof(Pages.Settings));

                System.Diagnostics.Debug.WriteLine("AppShell initialisé avec succès");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans AppShell: {ex.Message}");
                throw;
            }
        }
    }
}
