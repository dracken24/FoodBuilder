﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace FoodBuilder.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MealPlan : ContentPage
    {
        public MealPlan()
        {
            InitializeComponent();
        }
    }
}