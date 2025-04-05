using UnityEngine;
using VContainer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.Assets.Scripts.Data
{
    public class CurrencyManager
    {
        public event Action<string, long> OnCurrencyChanged;
        
        [Inject] private IDataRepository _dataRepository;
        private Dictionary<string, long> _currencies = new Dictionary<string, long>();

        public long GetCurrency(string currencyType)
        {
            return _currencies.TryGetValue(currencyType, out long amount) ? amount : 0;
        }

        public async Task UpdateCurrency(string currencyType, long amount)
        {
            _currencies[currencyType] = amount;
            await _dataRepository.SaveCurrencies(_currencies);
            OnCurrencyChanged?.Invoke(currencyType, amount);
        }

        public async Task AddCurrency(string currencyType, long amount)
        {
            if (!_currencies.ContainsKey(currencyType))
                _currencies[currencyType] = 0;
                
            _currencies[currencyType] += amount;
            await _dataRepository.SaveCurrencies(_currencies);
            OnCurrencyChanged?.Invoke(currencyType, _currencies[currencyType]);
        }

        public Dictionary<string, long> GetAllCurrencies()
        {
            return new Dictionary<string, long>(_currencies);
        }

        public void InitializeCurrencies(Dictionary<string, long> currencies)
        {
            _currencies = new Dictionary<string, long>(currencies);
            foreach (var currency in _currencies)
            {
                OnCurrencyChanged?.Invoke(currency.Key, currency.Value);
            }
        }
    }
} 