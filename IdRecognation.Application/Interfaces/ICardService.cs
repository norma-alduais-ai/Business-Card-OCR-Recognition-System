using Application.DTOs;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ICardService
    {
        Task<CardProcessResult> ProcessCardAsync(IFormFile imageFile);
        Task<List<CardDto>> GetAllAsync();
    }

    public class CardProcessResult
    {
        public bool Success { get; set; }
        public CardDto Data { get; set; }
        public string Error { get; set; }
    }
}