//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Sockets;
//using System.Text;
//using System.Threading.Tasks;

using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Th11s.AcmeServer.Tests.Services.ChallengeValidation.dns_01;

internal class SimpleDnsServer
{
    private static readonly Dictionary<string, List<string>> txtRecords = new Dictionary<string, List<string>>
    {
        { "example.com", new List<string> { "v=spf1 include:_spf.google.com ~all" } },
        { "test.example.com", new List<string> { "description=Test record", "contact=admin@test.example.com" } },
        { "no-txt-record.com", new List<string>() }
    };

    static void NotMain(string[] args)
    {
        UdpClient listener = new UdpClient(53);
        IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, 53);

        Console.WriteLine("Simple DNS Server started. Listening for DNS queries...");

        while (true)
        {
            byte[] bytes = listener.Receive(ref groupEP);
            string receivedData = Encoding.UTF8.GetString(bytes);

            // Parse the DNS query
            string domainName = ExtractDomainNameFromQuery(bytes);
            Console.WriteLine($"Received query for: {domainName}");

            // Create a DNS response
            byte[] response = CreateTxtRecordResponse(bytes, domainName);

            // Send the DNS response back to the client
            listener.Send(response, response.Length, groupEP);
        }
    }

    private static string ExtractDomainNameFromQuery(byte[] query)
    {
        int index = 12; // Start after the header
        StringBuilder domainName = new StringBuilder();

        while (query[index] != 0)
        {
            int length = query[index];
            index++;

            for (int i = 0; i < length; i++)
            {
                domainName.Append((char)query[index + i]);
            }

            domainName.Append('.');
            index += length;
        }

        return domainName.ToString().TrimEnd('.');
    }

    private static byte[] CreateTxtRecordResponse(byte[] query, string domainName)
    {
        List<byte> response = new List<byte>(query); // Start with the original query

        // Set the QR bit to 1 (response)
        response[2] |= 0x80;

        // Set the RCODE to 0 (no error)
        response[3] &= 0xF0;

        // Set the ANCOUNT to 1 (one answer record)
        response[6] = 0x00;
        response[7] = 0x01;

        // Add the answer section
        int index = 12; // Start after the header

        // Copy the question section
        while (query[index] != 0)
        {
            response.Add(query[index]);
            index++;
        }
        response.Add(0); // End of domain name

        // Type (TXT)
        response.AddRange(new byte[] { 0x00, 0x10 });

        // Class (IN)
        response.AddRange(new byte[] { 0x00, 0x01 });

        // TTL (4 bytes, set to 3600 seconds)
        response.AddRange(new byte[] { 0x00, 0x00, 0x0E, 0x10 });

        // RDLENGTH (to be filled later)
        int rdLengthIndex = response.Count;
        response.AddRange(new byte[] { 0x00, 0x00 });

        // RDATA (TXT records)
        List<string> txts = txtRecords.ContainsKey(domainName) ? txtRecords[domainName] : new List<string>();
        int rdLength = 0;

        foreach (string txt in txts)
        {
            if (txt.Length > 255) continue; // TXT record must be less than 256 characters

            response.Add((byte)txt.Length);
            response.AddRange(Encoding.UTF8.GetBytes(txt));
            rdLength += txt.Length + 1;
        }

        // Fill in the RDLENGTH
        response[rdLengthIndex] = (byte)(rdLength >> 8);
        response[rdLengthIndex + 1] = (byte)(rdLength & 0xFF);

        return response.ToArray();
    }
}