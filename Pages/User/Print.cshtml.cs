
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using System.Threading.Tasks;
using System;

namespace Barangay.Pages.User
{
    [Authorize]
    public class PrintModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PrintModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Prescription Prescription { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Prescription = await _context.Prescriptions
                .Include(p => p.Doctor)
                .Include(p => p.Patient)
                    .ThenInclude(p => p.User)
                .Include(p => p.PrescriptionMedicines)
                    .ThenInclude(pm => pm.Medication)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Prescription == null)
            {
                return NotFound();
            }
            
            return Page();
        }
        
        
        private int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}
