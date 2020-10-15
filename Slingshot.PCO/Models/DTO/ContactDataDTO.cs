using System.Collections.Generic;

namespace Slingshot.PCO.Models.DTO
{
    public class ContactDataDTO
    {
        public List<AddressDTO> Addresses { get; set; }

        public List<EmailAddressDTO> EmailAddresses { get; set; }

        public List<PhoneNumberDTO> PhoneNumbers { get; set; }

        public ContactDataDTO()
        {
            Addresses = new List<AddressDTO>();
            EmailAddresses = new List<EmailAddressDTO>();
            PhoneNumbers = new List<PhoneNumberDTO>();
        }
    }
}
