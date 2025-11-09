using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Application.DTOs;
using Domain.Entities;

namespace Application.Converters
{
    public static class CardConverter
    {
        public static CardDto ToDto(this Card e) => new CardDto
        {
            Id = e.Id,
            Name = e.Name,
            Email = e.Email,
            Phone = e.Phone,
            Company = e.Company,
            CreatedAt = e.CreatedAt
        };

        public static Card ToEntity(this CardDto d) => new Card
        {
            Id = d.Id,
            Name = d.Name,
            Email = d.Email,
            Phone = d.Phone,
            Company = d.Company,
            CreatedAt = d.CreatedAt
        };
    }
}

