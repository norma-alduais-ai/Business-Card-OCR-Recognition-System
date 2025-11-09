using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class CardRepository : ICardRepository
    {
        private readonly AppDbContext _db;
        public CardRepository(AppDbContext db) => _db = db;

        public async Task AddAsync(Card card, CancellationToken ct = default)
        {
            await _db.Cards.AddAsync(card, ct);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<List<Card>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.Cards.OrderByDescending(c => c.CreatedAt).ToListAsync(ct);
        }
    }
}
