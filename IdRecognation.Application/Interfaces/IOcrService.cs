using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IOcrService
    {
        /// <summary>
        /// Extracts text from the provided image stream.
        /// Implementations may preprocess the image.
        /// </summary>
        string ExtractText(Stream imageStream);
    }
}
