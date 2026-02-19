using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;

namespace ClinicManagementSystem.Controllers
{
    public class PatientInvoicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientInvoicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PatientInvoices
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, int? doctorId, string? paymentStatus)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var sessionDoctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            IQueryable<PatientInvoice> query = _context.PatientInvoices
                .Include(i => i.Patient)
                .Include(i => i.Doctor)
                .Include(i => i.Items);

            // Reception and Assistant see only their doctor's group invoices
            if (userType == SessionHelper.TYPE_RECEPTION || userType == SessionHelper.TYPE_ASSISTANT || userType == SessionHelper.TYPE_DOCTOR)
            {
                if (!sessionDoctorId.HasValue)
                    return View(new List<PatientInvoice>());

                var groupDoctorIds = await _context.GetGroupDoctorIdsAsync(sessionDoctorId.Value);
                query = query.Where(i => groupDoctorIds.Contains(i.DoctorId));
            }

            if (fromDate.HasValue)
                query = query.Where(i => i.InvoiceDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(i => i.InvoiceDate <= toDate.Value);

            if (doctorId.HasValue)
                query = query.Where(i => i.DoctorId == doctorId.Value);

            if (!string.IsNullOrEmpty(paymentStatus))
                query = query.Where(i => i.PaymentStatus == paymentStatus);

            var invoices = await query.OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.Id).ToListAsync();

            // Totals for summary
            ViewBag.TotalAmount = invoices.Sum(i => i.NetAmount);
            ViewBag.TotalPaid = invoices.Sum(i => i.PaidAmount);
            ViewBag.TotalRemaining = invoices.Sum(i => i.RemainingAmount);

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.SelectedDoctor = doctorId;
            ViewBag.SelectedPaymentStatus = paymentStatus ?? "";

            await PopulateDoctorsDropdown(doctorId);
            return View(invoices);
        }

        // GET: PatientInvoices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var invoice = await _context.PatientInvoices
                .Include(i => i.Patient)
                .Include(i => i.Doctor)
                .Include(i => i.Appointment)
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();

            if (!await CanAccessInvoiceAsync(invoice))
            {
                TempData["Error"] = "You don't have permission to view this invoice.";
                return RedirectToAction(nameof(Index));
            }

            return View(invoice);
        }

        // GET: PatientInvoices/Print/5
        public async Task<IActionResult> Print(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var invoice = await _context.PatientInvoices
                .Include(i => i.Patient)
                .Include(i => i.Doctor)
                .Include(i => i.Appointment)
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();

            if (!await CanAccessInvoiceAsync(invoice))
            {
                TempData["Error"] = "You don't have permission to print this invoice.";
                return RedirectToAction(nameof(Index));
            }

            return View(invoice);
        }

        // GET: PatientInvoices/Create
        public async Task<IActionResult> Create(int? patientId, int? appointmentId)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var sessionDoctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            var invoice = new PatientInvoice
            {
                InvoiceDate = DateTime.Today,
                InvoiceNumber = await GenerateInvoiceNumberAsync()
            };

            if (patientId.HasValue)
                invoice.PatientId = patientId.Value;

            if (appointmentId.HasValue)
            {
                invoice.AppointmentId = appointmentId.Value;
                var appt = await _context.Appointments.FindAsync(appointmentId.Value);
                if (appt != null)
                {
                    invoice.PatientId = appt.PatientId;
                    invoice.DoctorId = appt.DoctorId;
                }
            }

            if (userType == SessionHelper.TYPE_DOCTOR && sessionDoctorId.HasValue)
                invoice.DoctorId = sessionDoctorId.Value;

            await PopulateDropdownsAsync(invoice.PatientId, invoice.DoctorId);
            return View(invoice);
        }

        // POST: PatientInvoices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PatientInvoice invoice,
            List<string> serviceNames, List<string?> descriptions,
            List<int> quantities, List<decimal> unitPrices)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var sessionDoctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            // Auto-assign doctor for doctor user type
            if (userType == SessionHelper.TYPE_DOCTOR && sessionDoctorId.HasValue)
                invoice.DoctorId = sessionDoctorId.Value;

            invoice.CreatedBy = userId;
            invoice.CreatedByType = userType ?? string.Empty;
            invoice.CreatedDate = DateTime.Now;

            // Build invoice items from posted arrays
            var items = new List<PatientInvoiceItem>();
            for (int i = 0; i < serviceNames.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(serviceNames[i]))
                {
                    items.Add(new PatientInvoiceItem
                    {
                        ServiceName = serviceNames[i],
                        Description = i < descriptions.Count ? descriptions[i] : null,
                        Quantity = i < quantities.Count ? quantities[i] : 1,
                        UnitPrice = i < unitPrices.Count ? unitPrices[i] : 0
                    });
                }
            }

            if (!items.Any())
            {
                ModelState.AddModelError(string.Empty, "At least one service item is required.");
            }

            // Determine payment status
            var totalNet = items.Sum(x => x.Quantity * x.UnitPrice) - invoice.Discount;
            if (invoice.PaidAmount <= 0)
                invoice.PaymentStatus = "Unpaid";
            else if (invoice.PaidAmount >= totalNet)
                invoice.PaymentStatus = "Paid";
            else
                invoice.PaymentStatus = "Partial";

            // Remove Items from ModelState to avoid validation conflict
            ModelState.Remove("Items");

            if (ModelState.IsValid)
            {
                invoice.Items = items;
                _context.Add(invoice);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Invoice {invoice.InvoiceNumber} created successfully.";
                return RedirectToAction(nameof(Details), new { id = invoice.Id });
            }

            await PopulateDropdownsAsync(invoice.PatientId, invoice.DoctorId);
            return View(invoice);
        }

        // POST: PatientInvoices/AddPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPayment(int id, decimal amount, string? paymentMethod)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var invoice = await _context.PatientInvoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();

            if (!await CanAccessInvoiceAsync(invoice))
            {
                TempData["Error"] = "You don't have permission to update this invoice.";
                return RedirectToAction(nameof(Index));
            }

            if (amount <= 0)
            {
                TempData["Error"] = "Payment amount must be greater than zero.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var remaining = invoice.NetAmount - invoice.PaidAmount;
            if (amount > remaining)
            {
                TempData["Error"] = $"Payment amount ({amount:N3}) exceeds remaining balance ({remaining:N3}).";
                return RedirectToAction(nameof(Details), new { id });
            }

            invoice.PaidAmount += amount;

            if (!string.IsNullOrEmpty(paymentMethod))
                invoice.PaymentMethod = paymentMethod;

            var net = invoice.NetAmount;
            if (invoice.PaidAmount <= 0)
                invoice.PaymentStatus = "Unpaid";
            else if (invoice.PaidAmount >= net)
                invoice.PaymentStatus = "Paid";
            else
                invoice.PaymentStatus = "Partial";

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Payment of {amount:N3} recorded successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: PatientInvoices/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var invoice = await _context.PatientInvoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();

            if (!await CanAccessInvoiceAsync(invoice))
            {
                TempData["Error"] = "You don't have permission to edit this invoice.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(invoice.PatientId, invoice.DoctorId);
            return View(invoice);
        }

        // POST: PatientInvoices/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PatientInvoice invoice,
            List<string> serviceNames, List<string?> descriptions,
            List<int> quantities, List<decimal> unitPrices)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id != invoice.Id) return NotFound();

            var existing = await _context.PatientInvoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (existing == null) return NotFound();

            if (!await CanAccessInvoiceAsync(existing))
            {
                TempData["Error"] = "You don't have permission to edit this invoice.";
                return RedirectToAction(nameof(Index));
            }

            var items = new List<PatientInvoiceItem>();
            for (int i = 0; i < serviceNames.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(serviceNames[i]))
                {
                    items.Add(new PatientInvoiceItem
                    {
                        ServiceName = serviceNames[i],
                        Description = i < descriptions.Count ? descriptions[i] : null,
                        Quantity = i < quantities.Count ? quantities[i] : 1,
                        UnitPrice = i < unitPrices.Count ? unitPrices[i] : 0
                    });
                }
            }

            if (!items.Any())
            {
                ModelState.AddModelError(string.Empty, "At least one service item is required.");
            }

            var totalNet = items.Sum(x => x.Quantity * x.UnitPrice) - invoice.Discount;
            if (invoice.PaidAmount <= 0)
                invoice.PaymentStatus = "Unpaid";
            else if (invoice.PaidAmount >= totalNet)
                invoice.PaymentStatus = "Paid";
            else
                invoice.PaymentStatus = "Partial";

            ModelState.Remove("Items");

            if (ModelState.IsValid)
            {
                // Preserve creation info
                invoice.CreatedBy = existing.CreatedBy;
                invoice.CreatedByType = existing.CreatedByType;
                invoice.CreatedDate = existing.CreatedDate;
                invoice.InvoiceNumber = existing.InvoiceNumber;

                // Remove old items and add new ones
                _context.PatientInvoiceItems.RemoveRange(existing.Items);

                existing.PatientId = invoice.PatientId;
                existing.DoctorId = invoice.DoctorId;
                existing.AppointmentId = invoice.AppointmentId;
                existing.InvoiceDate = invoice.InvoiceDate;
                existing.Discount = invoice.Discount;
                existing.PaidAmount = invoice.PaidAmount;
                existing.PaymentStatus = invoice.PaymentStatus;
                existing.PaymentMethod = invoice.PaymentMethod;
                existing.Notes = invoice.Notes;
                existing.Items = items;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Invoice updated successfully.";
                return RedirectToAction(nameof(Details), new { id = invoice.Id });
            }

            await PopulateDropdownsAsync(invoice.PatientId, invoice.DoctorId);
            invoice.Items = items;
            return View(invoice);
        }

        // GET: PatientInvoices/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var invoice = await _context.PatientInvoices
                .Include(i => i.Patient)
                .Include(i => i.Doctor)
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();

            if (!await CanAccessInvoiceAsync(invoice))
            {
                TempData["Error"] = "You don't have permission to delete this invoice.";
                return RedirectToAction(nameof(Index));
            }

            return View(invoice);
        }

        // POST: PatientInvoices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var invoice = await _context.PatientInvoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();

            if (!await CanAccessInvoiceAsync(invoice))
            {
                TempData["Error"] = "You don't have permission to delete this invoice.";
                return RedirectToAction(nameof(Index));
            }

            _context.PatientInvoices.Remove(invoice);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Invoice deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: PatientInvoices/DoctorIncome
        public async Task<IActionResult> DoctorIncome(DateTime? fromDate, DateTime? toDate, int? doctorId)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var sessionDoctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            // Default to current month
            fromDate ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            toDate ??= DateTime.Today;

            IQueryable<PatientInvoice> query = _context.PatientInvoices
                .Include(i => i.Doctor)
                .Include(i => i.Items)
                .Where(i => i.InvoiceDate >= fromDate && i.InvoiceDate <= toDate);

            if (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_RECEPTION || userType == SessionHelper.TYPE_ASSISTANT)
            {
                if (!sessionDoctorId.HasValue)
                    return View(new List<DoctorIncomeViewModel>());

                var groupDoctorIds = await _context.GetGroupDoctorIdsAsync(sessionDoctorId.Value);
                query = query.Where(i => groupDoctorIds.Contains(i.DoctorId));
            }

            if (doctorId.HasValue)
                query = query.Where(i => i.DoctorId == doctorId.Value);

            var invoices = await query.ToListAsync();

            var report = invoices
                .GroupBy(i => new { i.DoctorId, DoctorName = i.Doctor?.DoctorName ?? "Unknown" })
                .Select(g => new DoctorIncomeViewModel
                {
                    DoctorId = g.Key.DoctorId,
                    DoctorName = g.Key.DoctorName,
                    InvoiceCount = g.Count(),
                    TotalAmount = g.Sum(i => i.NetAmount),
                    TotalPaid = g.Sum(i => i.PaidAmount),
                    TotalRemaining = g.Sum(i => i.RemainingAmount)
                })
                .OrderByDescending(r => r.TotalPaid)
                .ToList();

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            ViewBag.SelectedDoctor = doctorId;

            await PopulateDoctorsDropdown(doctorId);
            return View(report);
        }

        // ---- Helpers ----

        private async Task<bool> CanAccessInvoiceAsync(PatientInvoice invoice)
        {
            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var sessionDoctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            if (userType == SessionHelper.TYPE_ADMIN)
                return true;

            if ((userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_RECEPTION || userType == SessionHelper.TYPE_ASSISTANT) && sessionDoctorId.HasValue)
            {
                var groupDoctorIds = await _context.GetGroupDoctorIdsAsync(sessionDoctorId.Value);
                return groupDoctorIds.Contains(invoice.DoctorId);
            }

            return false;
        }

        private async Task PopulateDropdownsAsync(int? selectedPatient = null, int? selectedDoctor = null)
        {
            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var sessionDoctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            IQueryable<Patient> patientsQuery = _context.Patients;
            IQueryable<DoctorInfo> doctorsQuery = _context.DoctorInfos.Where(d => d.Active);

            if (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_RECEPTION || userType == SessionHelper.TYPE_ASSISTANT)
            {
                if (sessionDoctorId.HasValue)
                {
                    var groupDoctorIds = await _context.GetGroupDoctorIdsAsync(sessionDoctorId.Value);
                    patientsQuery = patientsQuery.Where(p => p.DoctorId.HasValue && groupDoctorIds.Contains(p.DoctorId.Value));
                    doctorsQuery = doctorsQuery.Where(d => groupDoctorIds.Contains(d.Id));
                }
            }

            ViewBag.PatientId = new SelectList(patientsQuery.OrderBy(p => p.PatientName), "Id", "PatientName", selectedPatient);
            ViewBag.DoctorId = new SelectList(doctorsQuery.OrderBy(d => d.DoctorName), "Id", "DoctorName", selectedDoctor);

            ViewBag.AppointmentId = new SelectList(
                _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => !a.IsDeleted)
                    .OrderByDescending(a => a.AppointmentDate)
                    .Select(a => new { a.Id, Label = a.Patient!.PatientName + " - " + a.AppointmentDate.ToString("yyyy-MM-dd") }),
                "Id", "Label", null);
        }

        private async Task PopulateDoctorsDropdown(int? selectedDoctor = null)
        {
            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var sessionDoctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            IQueryable<DoctorInfo> doctorsQuery = _context.DoctorInfos.Where(d => d.Active);

            if (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_RECEPTION || userType == SessionHelper.TYPE_ASSISTANT)
            {
                if (sessionDoctorId.HasValue)
                {
                    var groupDoctorIds = await _context.GetGroupDoctorIdsAsync(sessionDoctorId.Value);
                    doctorsQuery = doctorsQuery.Where(d => groupDoctorIds.Contains(d.Id));
                }
            }

            ViewBag.DoctorList = new SelectList(doctorsQuery.OrderBy(d => d.DoctorName), "Id", "DoctorName", selectedDoctor);
        }

        private async Task<string> GenerateInvoiceNumberAsync()
        {
            var today = DateTime.Today;
            var prefix = $"INV-{today:yyyyMMdd}-";
            var count = await _context.PatientInvoices
                .CountAsync(i => i.InvoiceNumber.StartsWith(prefix));
            return $"{prefix}{(count + 1):D3}";
        }
    }

    // ViewModel for doctor income report
    public class DoctorIncomeViewModel
    {
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalRemaining { get; set; }
    }
}
