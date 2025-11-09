using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Repositories;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Application.Services
{
    public class CardService : ICardService
    {
        private readonly ICardRepository _repo;
        private readonly IOcrService _ocr;

        public CardService(ICardRepository repo, IOcrService ocr)
        {
            _repo = repo;
            _ocr = ocr;
        }

        public async Task<CardProcessResult> ProcessCardAsync(IFormFile imageFile)
        {
            try
            {
                // SECURITY: Validate file first
                var validationResult = ValidateImageFile(imageFile);
                if (!validationResult.IsValid)
                {
                    return new CardProcessResult
                    {
                        Success = false,
                        Data = null,
                        Error = validationResult.ErrorMessage
                    };
                }

                // Convert to memory stream to preserve data
                using var memoryStream = new MemoryStream();
                await imageFile.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var cardDto = await ProcessAndSaveAsync(memoryStream);

                return new CardProcessResult
                {
                    Success = true,
                    Data = cardDto,
                    Error = null
                };
            }
            catch (Exception ex)
            {
                // SECURITY: Don't expose internal errors
                return new CardProcessResult
                {
                    Success = false,
                    Data = null,
                    Error = "Processing failed. Please try again."
                };
            }
        }

        // SECURITY: File validation
        private (bool IsValid, string ErrorMessage) ValidateImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return (false, "Please select a file.");

            // File size limit (5MB)
            if (file.Length > 5 * 1024 * 1024)
                return (false, "File size too large. Maximum size is 5MB.");

            // Allowed extensions
            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tiff" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
                return (false, "Invalid file format. Supported formats: PNG, JPG, JPEG, BMP, TIFF.");

            // SECURITY: Check file signature (basic MIME type validation)
            try
            {
                using var stream = file.OpenReadStream();
                byte[] header = new byte[8];
                stream.Read(header, 0, 8);

                if (!IsValidImageSignature(header))
                    return (false, "Invalid image file.");
            }
            catch
            {
                return (false, "Cannot read file. Please try another image.");
            }

            return (true, null);
        }

        // SECURITY: Basic image signature validation
        private bool IsValidImageSignature(byte[] header)
        {
            // PNG: 89 50 4E 47
            bool isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;

            // JPEG: FF D8 FF
            bool isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;

            // BMP: 42 4D
            bool isBmp = header[0] == 0x42 && header[1] == 0x4D;

            return isPng || isJpeg || isBmp;
        }

        public async Task<CardDto> ProcessAndSaveAsync(Stream imageStream)
        {
            var text = _ocr.ExtractText(imageStream) ?? string.Empty;
            var parsed = ParseBusinessCardText(text);

            // SECURITY: Sanitize data before saving
            var card = new Card
            {
                Name = SanitizeInput(parsed.Name, 100),        // Limit length
                Email = SanitizeInput(parsed.Email, 100),      // Limit length
                Phone = ValidateAndFormatPhone(parsed.Phone),  // Validate phone
                Company = SanitizeInput(parsed.Company, 100)   // Limit length
            };

            await _repo.AddAsync(card);
            return new CardDto
            {
                Name = card.Name,
                Email = card.Email,
                Phone = card.Phone,
                Company = card.Company
            };
        }

        // SECURITY: Input sanitization
        private string? SanitizeInput(string? input, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            // Remove potentially dangerous characters
            var sanitized = Regex.Replace(input, @"[<>""'&]", "");

            // Trim and limit length
            sanitized = sanitized.Trim();
            if (sanitized.Length > maxLength)
                sanitized = sanitized.Substring(0, maxLength);

            return sanitized;
        }

        // SECURITY: Phone number validation and formatting
        private string? ValidateAndFormatPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            // Remove all non-digit characters except +
            var digitsOnly = Regex.Replace(phone, @"[^\d+]", "");

            // SECURITY: Validate phone number format
            if (!IsValidPhoneNumber(digitsOnly))
                return null;

            // SECURITY: Limit phone number length
            if (digitsOnly.Length > 15)
                return null;

            return digitsOnly;
        }

        // SECURITY: Strict phone number validation
        private bool IsValidPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // Must be between 7 and 15 digits (international standards)
            if (phone.Length < 7 || phone.Length > 15)
                return false;

            // Must contain only digits (except optional + at start)
            if (!Regex.IsMatch(phone, @"^\+?\d+$"))
                return false;

            // SECURITY: Block suspicious patterns
            var suspiciousPatterns = new[]
            {
                @"^\d{1,6}$",           // Too short
                @"^0+$",                // All zeros
                @"^1+$",                // All ones
                @"^1234567",            // Sequential
                @"^999",                // Test patterns
                @"^000"                 // Suspicious
            };

            if (suspiciousPatterns.Any(pattern => Regex.IsMatch(phone, pattern)))
                return false;

            return true;
        }

        private (string? Name, string? Email, string? Phone, string? Company) ParseBusinessCardText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (null, null, null, null);

            // SECURITY: Limit input size to prevent DoS
            if (text.Length > 10000)
                text = text.Substring(0, 10000);

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(l => l.Trim())
                            .Where(l => !string.IsNullOrWhiteSpace(l))
                            .Take(20) // SECURITY: Limit number of lines processed
                            .ToList();

            string? name = null;
            string? email = null;
            string? phone = null;
            string? company = null;

            foreach (var line in lines)
            {
                // SECURITY: Limit line length
                if (line.Length > 200)
                    continue;

                // Extract email with validation
                if (email == null)
                {
                    var emailMatch = Regex.Match(line, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b");
                    if (emailMatch.Success && emailMatch.Value.Length <= 100)
                        email = emailMatch.Value;
                }

                // Extract phone with security validation
                if (phone == null)
                {
                    var phoneMatch = ExtractPhoneNumber(line);
                    if (phoneMatch != null && IsValidPhoneNumber(phoneMatch))
                        phone = phoneMatch;
                }

                // Company detection
                if (company == null && ContainsBusinessKeywords(line) && line != email && line != phone)
                {
                    company = line.Length <= 100 ? line : line.Substring(0, 100);
                }
            }

            // Name detection with security
            name = lines.FirstOrDefault(line =>
                !IsEmail(line) &&
                !IsPhone(line) &&
                !ContainsBusinessKeywords(line) &&
                line.Length > 2 &&
                line.Length <= 100
            );

            return (name, email, phone, company);
        }

        private string? ExtractPhoneNumber(string text)
        {
            // SECURITY: Limit input size
            if (text.Length > 100)
                text = text.Substring(0, 100);

            var cleanText = text.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "");

            // Pattern 1: Simple long numbers (7-15 digits)
            var simpleMatch = Regex.Match(cleanText, @"\b\d{7,15}\b");
            if (simpleMatch.Success)
                return simpleMatch.Value;

            // Pattern 2: International format
            var internationalMatch = Regex.Match(cleanText, @"\+?[1-9]\d{6,14}");
            if (internationalMatch.Success)
                return internationalMatch.Value;

            // Pattern 3: Formatted numbers
            var formattedMatch = Regex.Match(text, @"\b\d{3}[-.\s]?\d{3}[-.\s]?\d{4,5}\b");
            if (formattedMatch.Success)
                return formattedMatch.Value.Replace(" ", "").Replace("-", "").Replace(".", "");

            return null;
        }

        private bool ContainsBusinessKeywords(string text)
        {
            var keywords = new[] { "inc", "corp", "company", "ltd", "llc", "gmbh", "co", "group", "enterprises", "technologies", "solutions", "consulting", "services", "corporation", "limited" };
            return keywords.Any(keyword => text.ToLower().Contains(keyword));
        }

        private bool IsEmail(string text) => Regex.IsMatch(text, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b");

        private bool IsPhone(string text)
        {
            var cleanText = text.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "");
            return Regex.IsMatch(cleanText, @"\b\d{7,15}\b") ||
                   Regex.IsMatch(cleanText, @"\+?[1-9]\d{6,14}");
        }

        public async Task<List<CardDto>> GetAllAsync()
        {
            var list = await _repo.GetAllAsync();
            return list.Select(l => new CardDto
            {
                Name = l.Name,
                Email = l.Email,
                Phone = l.Phone,
                Company = l.Company
            }).ToList();
        }
    }
}