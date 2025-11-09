using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Repositories
{
    public interface ICardRepository
    {
        Task AddAsync(Card card, CancellationToken ct = default);
        Task<List<Card>> GetAllAsync(CancellationToken ct = default);
    }
}
