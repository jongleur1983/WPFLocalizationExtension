﻿namespace WPFLocalizationExtensionDemoApplication.ViewModels.Examples
{
    using System;
    using System.Linq;
    using Caliburn.Micro;

    public class GapTextWpfExampleViewModel : Screen
    {
        private string _city;
        private DayOfWeek _weekDay;
        private DateTime _openingTime;
        private DateTime _closingTime;
        private Animal _selectedAnimal;

        public GapTextWpfExampleViewModel()
        {
            base.DisplayName = "GapTextExample";
            this.City = "Berlin";
            this.WeekDay = DayOfWeek.Monday;

            var million = 1000 * 1000;

            this.Animals = new BindableCollection<Animal>
            {
                new Animal("Dodo", DateTime.Now.Year - 1681, 1681),
                new Animal("Stegosaurus", 195 * million),
                new Animal("Triceratops", 66 * million),
                new Animal("Diplodocus", 147 * million),
                new Animal("Apatosaurus", 145 * million)
            };

            this.SelectedAnimal = this.Animals.First();

            this.NotifyOfPropertyChange(() => this.Animals);
        }

        public string City
        {
            get { return _city; }
            set
            {
                if (value != _city)
                {
                    _city = value;
                    NotifyOfPropertyChange(() => City);
                }
            }
        }

        public DayOfWeek WeekDay
        {
            get { return _weekDay; }
            set
            {
                if (value != _weekDay)
                {
                    _weekDay = value;
                    NotifyOfPropertyChange(() => WeekDay);
                }
            }
        }

        public BindableCollection<DayOfWeek> WeekDays
        {
            get
            {
                return new BindableCollection<DayOfWeek>(Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>());
            }
        }

        public DateTime OpeningTime
        {
            get { return _openingTime; }
            set
            {
                if (value.Equals(_openingTime)) return;
                _openingTime = value;
                NotifyOfPropertyChange(() => OpeningTime);
            }
        }

        public DateTime ClosingTime
        {
            get { return _closingTime; }
            set
            {
                if (value.Equals(_closingTime)) return;
                _closingTime = value;
                NotifyOfPropertyChange(() => ClosingTime);
            }
        }

        public class Animal
        {
            public Animal(string name, int age, int? lastSeen = null)
            {
                this.Name = name;
                this.Age = age;
                this.LastSeen = lastSeen;
            }

            public string Name { get; set; }

            public int Age { get; set; }

            public int? LastSeen { get; set; }
        }

        public BindableCollection<Animal> Animals { get; set; }

        public Animal SelectedAnimal
        {
            get
            {
                return _selectedAnimal;
            }
            set
            {
                if (Equals(value, _selectedAnimal)) return;
                _selectedAnimal = value;
                NotifyOfPropertyChange(() => SelectedAnimal);
                NotifyOfPropertyChange(() => this.DynamicFormatString);
            }
        }

        public string DynamicFormatString
        {
            get
            {
                if (this.SelectedAnimal.LastSeen != null)
                {
                    return "The last {0} has been seen in {1}.";
                }
                else
                {
                    return string.Empty;
                }
            }
        }
    }
}
