using System.ComponentModel.DataAnnotations;

namespace NhanViet.Companies.ViewModels
{
    /// <summary>
    /// ViewModel for displaying Company data
    /// </summary>
    public class CompanyViewModel
    {
        public string ContentItemId { get; set; }
        public string CompanyName { get; set; }
        public string Description { get; set; }
        public string Industry { get; set; }
        public string CompanySize { get; set; }
        public string Website { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
        public int? FoundedYear { get; set; }
        public bool IsVerified { get; set; }
        public bool IsFeatured { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        public DateTime? CreatedUtc { get; set; }
        public DateTime? ModifiedUtc { get; set; }
        public bool Published { get; set; }
    }

    /// <summary>
    /// ViewModel for creating new Company
    /// </summary>
    public class CreateCompanyViewModel
    {
        [Required(ErrorMessage = "Company name is required")]
        [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters")]
        public string CompanyName { get; set; }

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; }

        [StringLength(100, ErrorMessage = "Industry cannot exceed 100 characters")]
        public string Industry { get; set; }

        [StringLength(50, ErrorMessage = "Company size cannot exceed 50 characters")]
        public string CompanySize { get; set; }

        [Url(ErrorMessage = "Please enter a valid website URL")]
        [StringLength(500, ErrorMessage = "Website URL cannot exceed 500 characters")]
        public string Website { get; set; }

        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(200, ErrorMessage = "Email cannot exceed 200 characters")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [StringLength(50, ErrorMessage = "Phone cannot exceed 50 characters")]
        public string Phone { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string City { get; set; }

        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters")]
        public string Country { get; set; }

        [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters")]
        public string PostalCode { get; set; }

        [Range(1800, 2100, ErrorMessage = "Founded year must be between 1800 and 2100")]
        public int? FoundedYear { get; set; }
    }

    /// <summary>
    /// ViewModel for updating Company
    /// </summary>
    public class UpdateCompanyViewModel
    {
        [Required(ErrorMessage = "Company name is required")]
        [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters")]
        public string CompanyName { get; set; }

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; }

        [StringLength(100, ErrorMessage = "Industry cannot exceed 100 characters")]
        public string Industry { get; set; }

        [StringLength(50, ErrorMessage = "Company size cannot exceed 50 characters")]
        public string CompanySize { get; set; }

        [Url(ErrorMessage = "Please enter a valid website URL")]
        [StringLength(500, ErrorMessage = "Website URL cannot exceed 500 characters")]
        public string Website { get; set; }

        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(200, ErrorMessage = "Email cannot exceed 200 characters")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [StringLength(50, ErrorMessage = "Phone cannot exceed 50 characters")]
        public string Phone { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string City { get; set; }

        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters")]
        public string Country { get; set; }

        [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters")]
        public string PostalCode { get; set; }

        [Range(1800, 2100, ErrorMessage = "Founded year must be between 1800 and 2100")]
        public int? FoundedYear { get; set; }
    }

    /// <summary>
    /// ViewModel for Company search results
    /// </summary>
    public class CompanySearchResultViewModel
    {
        public string Query { get; set; }
        public IEnumerable<CompanyViewModel> Companies { get; set; } = new List<CompanyViewModel>();
        public int TotalResults { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalResults / PageSize);
    }

    /// <summary>
    /// ViewModel for Company directory/listing
    /// </summary>
    public class CompanyDirectoryViewModel
    {
        public IEnumerable<CompanyViewModel> Companies { get; set; } = new List<CompanyViewModel>();
        public IEnumerable<CompanyViewModel> FeaturedCompanies { get; set; } = new List<CompanyViewModel>();
        public Dictionary<string, int> IndustryFilter { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> SizeFilter { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> LocationFilter { get; set; } = new Dictionary<string, int>();
        public CompanyFilterOptions CurrentFilter { get; set; } = new CompanyFilterOptions();
    }

    /// <summary>
    /// Company filter options
    /// </summary>
    public class CompanyFilterOptions
    {
        public string? Industry { get; set; }
        public string? CompanySize { get; set; }
        public string? Location { get; set; }
        public bool? IsVerified { get; set; }
        public bool? IsFeatured { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public int? MinRating { get; set; }
        public int? MaxRating { get; set; }
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 20;
    }
}