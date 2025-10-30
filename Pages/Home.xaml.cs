using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Extensions.DependencyInjection;
using FoodBuilder.Services;
using FoodBuilder.Config;
using FoodBuilder.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace FoodBuilder.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Home : ContentPage
    {
        private readonly FirebaseAuthService _authService;
        private readonly FirestoreService _firestoreService;
        public ObservableCollection<Category> Categories { get; } = new ObservableCollection<Category>();

        public Home()
        {
            InitializeComponent();

            // Resolve services via MauiContext
            var services = this.Handler?.MauiContext?.Services ?? Application.Current?.Handler?.MauiContext?.Services;
            _authService = services!.GetService<FirebaseAuthService>()!;
            _firestoreService = services!.GetService<FirestoreService>()!;

            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                // Sign-in (email/pwd si fourni, sinon anonyme)
                if (!string.IsNullOrWhiteSpace(FirebaseConfig.TestEmail) && !string.IsNullOrWhiteSpace(FirebaseConfig.TestPassword))
                {
                    await _authService.SignInWithEmailAndPasswordAsync(FirebaseConfig.TestEmail!, FirebaseConfig.TestPassword!);
                }
                else
                {
                    await _authService.SignInAnonymouslyAsync();
                }

                // Charger les catégories
                List<Category> cats = await _firestoreService.GetCategoriesAsync();
                Categories.Clear();
                foreach (Category c in cats)
                {
                    Categories.Add(c);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Home] Error on appearing: {ex}");
                Console.WriteLine($"[Home] Error on appearing: {ex}");
                await DisplayAlert("Erreur", ex.ToString(), "OK");
            }
        }

        private async void OnCategoryTapped(object? sender, EventArgs e)
        {
            try
            {
                if (sender is Element element && element is BindableObject bindable)
                {
                    if (bindable.BindingContext is Category category)
                    {
                        await DisplayAlert("Catégorie", $"Vous avez sélectionné: {category.Name}", "OK");
                        // TODO: naviguer/filtrer selon votre besoin
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Home] OnCategoryTapped error: {ex}");
            }
        }
    }
}