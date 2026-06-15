using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SveshofReff.Data;
using SveshofReff.Models;
using System.Windows;

namespace SveshofReff.ViewModels
{
    public class MainViewModel : BindableBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITransactionRepository _transactionRepository;

        public MainViewModel()
        {
            _userRepository = new UserRepository();
            _transactionRepository = new TransactionRepository();

            Users = new ObservableCollection<User>();
            Referrals = new ObservableCollection<User>();
            Transactions = new ObservableCollection<Transaction>();

            SearchCommand = new RelayCommand(async _ => await SearchUsersAsync());
            AddUserCommand = new RelayCommand(async _ => await AddUserAsync(), _ => CanAddUser());
            ClearFormCommand = new RelayCommand(_ => ClearForm());
            DeleteUserCommand = new RelayCommand(async _ => await DeleteUserAsync(), _ => SelectedUser != null);

            // Инициализация при старте
            _ = LoadAllUsersAsync();
        }

        #region Свойства для коллекций и выбранного пользователя

        public ObservableCollection<User> Users { get; }
        public ObservableCollection<User> Referrals { get; }
        public ObservableCollection<Transaction> Transactions { get; }

        private User? _selectedUser;
        public User? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value))
                {
                    _ = LoadUserDetailsAsync(value);
                }
            }
        }

        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set => SetProperty(ref _searchQuery, value);
        }

        #endregion

        #region Свойства для формы добавления пользователя

        private string _newFullName = string.Empty;
        public string NewFullName
        {
            get => _newFullName;
            set => SetProperty(ref _newFullName, value);
        }

        private string _newPhone = string.Empty;
        public string NewPhone
        {
            get => _newPhone;
            set => SetProperty(ref _newPhone, value);
        }

        private string _newInviterCode = string.Empty;
        public string NewInviterCode
        {
            get => _newInviterCode;
            set => SetProperty(ref _newInviterCode, value);
        }

        #endregion

        #region Команды

        public ICommand SearchCommand { get; }
        public ICommand AddUserCommand { get; }
        public ICommand ClearFormCommand { get; }
        public ICommand DeleteUserCommand { get; }

        #endregion

        #region Приватные методы (Логика)

        private async Task LoadAllUsersAsync()
        {
            var users = await _userRepository.GetAllUsersAsync();
            Users.Clear();
            foreach (var u in users) Users.Add(u);
        }

        private async Task SearchUsersAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                await LoadAllUsersAsync();
                return;
            }

            var users = await _userRepository.SearchUsersAsync(SearchQuery);
            Users.Clear();
            foreach (var u in users) Users.Add(u);
        }

        private async Task LoadUserDetailsAsync(User? user)
        {
            Referrals.Clear();
            Transactions.Clear();

            if (user == null) return;

            var refs = await _userRepository.GetReferralsAsync(user.ID);
            foreach (var r in refs) Referrals.Add(r);

            var trans = await _transactionRepository.GetUserTransactionsAsync(user.ID);
            foreach (var t in trans) Transactions.Add(t);
        }

        private bool CanAddUser()
        {
            if (string.IsNullOrWhiteSpace(NewFullName) || string.IsNullOrWhiteSpace(NewPhone)) return false;
            
            // Оставляем только цифры для проверки
            string cleanPhone = new string(NewPhone.Where(char.IsDigit).ToArray());
            return cleanPhone.Length == 11;
        }

        private async Task AddUserAsync()
        {
            try
            {
                // Генерируем уникальный реферальный код для нового пользователя
                var newRefCode = GenerateReferralCode();
                var user = new User
                {
                    FullName = NewFullName,
                    PhoneNumber = NewPhone,
                    ReferralCode = newRefCode
                };

                await _userRepository.AddUserAsync(user, NewInviterCode);
                
                MessageBox.Show("Пользователь успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
                await LoadAllUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            NewFullName = string.Empty;
            NewPhone = string.Empty;
            NewInviterCode = string.Empty;
        }

        private string GenerateReferralCode()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }

        private async Task DeleteUserAsync()
        {
            if (SelectedUser == null) return;
            
            var result = MessageBox.Show($"Вы уверены, что хотите удалить пользователя {SelectedUser.FullName}?", "Удаление", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _userRepository.DeleteUserAsync(SelectedUser.ID);
                    SelectedUser = null;
                    await LoadAllUsersAsync();
                    MessageBox.Show("Пользователь удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }
}
