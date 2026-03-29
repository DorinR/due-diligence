using System.Text.RegularExpressions;

namespace rag_experiment.Services.FilingDownloader;

internal static class Fortune500CompanyFilter
{
    private static readonly Lazy<string[]> NormalizedNames = new(() =>
        Fortune500Companies
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Concat(AliasNames.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(Normalize)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray());

    private static readonly HashSet<string> IgnoredTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "and",
        "the",
        "of",
        "inc",
        "incorporated",
        "corp",
        "corporation",
        "co",
        "company",
        "companies",
        "group",
        "holdings",
        "holding",
        "plc",
        "ltd",
        "limited",
        "international",
        "technologies",
        "technology",
        "systems",
        "financial",
        "financials",
        "health",
        "healthcare",
        "communications",
        "services",
        "service",
        "brands",
        "resources",
        "enterprise",
        "enterprises"
    };

    public static bool IsIncluded(string companyName)
    {
        var normalizedCompanyName = Normalize(companyName);
        if (string.IsNullOrWhiteSpace(normalizedCompanyName))
        {
            return false;
        }

        return NormalizedNames.Value.Any(fortuneName => Matches(fortuneName, normalizedCompanyName));
    }

    private static bool Matches(string fortuneName, string secCompanyName)
    {
        if (fortuneName == secCompanyName)
        {
            return true;
        }

        var fortuneTokens = fortuneName.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var secTokens = secCompanyName.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (fortuneTokens.Length >= 2 &&
            (secCompanyName.Contains(fortuneName, StringComparison.OrdinalIgnoreCase) ||
                fortuneName.Contains(secCompanyName, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (fortuneTokens.Length == 1)
        {
            return secTokens.Contains(fortuneTokens[0], StringComparer.OrdinalIgnoreCase);
        }

        var fortuneTokenSet = fortuneTokens.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var secTokenSet = secTokens.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return fortuneTokenSet.Count >= 2 &&
            (fortuneTokenSet.IsSubsetOf(secTokenSet) || secTokenSet.IsSubsetOf(fortuneTokenSet));
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var cleaned = Regex.Replace(
            value.ToLowerInvariant().Replace("&", " and "),
            "[^a-z0-9 ]+",
            " ");

        var tokens = cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => !IgnoredTokens.Contains(token));

        return string.Join(' ', tokens);
    }

    private const string Fortune500Companies = """
Walmart
Amazon
UnitedHealth Group
Apple
CVS Health
Berkshire Hathaway
Alphabet
Exxon Mobil
McKesson
Cencora
JPMorgan Chase
Costco Wholesale
Cigna Group
Microsoft
Cardinal Health
Chevron
Bank of America
General Motors
Ford Motor
Elevance Health
Citigroup
Meta Platforms
Centene
Home Depot
Fannie Mae
Walgreens Boots Alliance
Kroger
Phillips 66
Marathon Petroleum
Verizon Communications
NVIDIA
Goldman Sachs
Wells Fargo
Valero Energy
Comcast
State Farm Insurance
AT&T
Freddie Mac
Humana
Morgan Stanley
Target
StoneX Group
Tesla
Dell Technologies
PepsiCo
Walt Disney
United Parcel Service
Johnson & Johnson
FedEx
Archer Daniels Midland
Procter & Gamble
Lowe's
Energy Transfer
RTX Corporation
Albertsons
Sysco
Progressive
American Express
Lockheed Martin
MetLife
HCA Healthcare
Prudential Financial
Boeing
Caterpillar
Merck
Allstate
Pfizer
IBM
New York Life Insurance
Delta Air Lines
Publix Super Markets
Nationwide
TD Synnex
United Airlines Holdings
ConocoPhillips
TJX Companies
AbbVie
Enterprise Products Partners
Charter Communications
Performance Food Group
American Airlines Group
Capital One Financial
Cisco Systems
HP Inc
Tyson Foods
Intel
Oracle
Broadcom
Deere & Company
Nike
Liberty Mutual Insurance Group
Plains GP Holdings
USAA
Bristol-Myers Squibb
Ingram Micro
General Dynamics
Coca-Cola
TIAA
Travelers Companies
Eli Lilly
AIG
Dow
Best Buy
Thermo Fisher Scientific
Northrop Grumman
CHS
Abbott Laboratories
LyondellBasell Industries
Qualcomm
Dollar General
GE Aerospace
Salesforce
T-Mobile US
Honeywell International
Molina Healthcare
US Foods Holding
Mondelez International
PBF Energy
Northwestern Mutual
Philip Morris International
Nucor
Jabil
PACCAR
MassMutual
Cummins
Amgen
Medtronic
3M
Dollar Tree
GE Vernova
Arrow Electronics
Hewlett Packard Enterprise
Duke Energy
HF Sinclair
Hartford Financial Services
Baker Hughes
Gilead Sciences
Kraft Heinz
Johnson Controls International
Applied Materials
Altria Group
Occidental Petroleum
Flex
Constellation Energy
Micron Technology
Ferguson Enterprises
Southern Company
NextEra Energy
US Bancorp
Danaher
Eaton
Macy's
World Kinect
Halliburton
AMD
Stryker
ONEOK
Exelon
Carrier Global
L3Harris Technologies
CDW
Ross Stores
Tenet Healthcare
Aflac
Parker Hannifin
Truist Financial
PNC Financial Services
General Mills
BJ's Wholesale Club
Becton Dickinson
Colgate-Palmolive
International Paper
Targa Resources
Freeport-McMoRan
Waste Management
Charles Schwab
Emerson Electric
PPG Industries
Steel Dynamics
Pilgrim's Pride
AutoZone
Trane Technologies
Illinois Tool Works
D.R. Horton
Lennar
Southwest Airlines
Raytheon Technologies
Cheniere Energy
Markel Group
Whirlpool
Leidos
Principal Financial Group
Texas Instruments
Vistra
Sempra
O'Reilly Automotive
Boston Scientific
Dominion Energy
United Natural Foods
Global Partners
American Electric Power
Kimberly-Clark
WestRock
Baxter International
Textron
Genuine Parts
Estee Lauder
CBRE Group
Republic Services
Corteva Agriscience
NRG Energy
Loews
Devon Energy
Marsh McLennan
DTE Energy
Automatic Data Processing
Ecolab
Xcel Energy
Conagra Brands
Henry Schein
Lincoln National
Consolidated Edison
Reinsurance Group of America
Sherwin-Williams
Penske Automotive Group
Celanese
Northrop Grumman Innovation Systems
Quanta Services
WR Berkley
Starbucks
Gap
Public Service Enterprise Group
Edison International
First Energy
Entergy
Nordstrom
Molson Coors Beverage
Unum Group
Kinder Morgan
Corning
Pacific Life
Lam Research
Interpublic Group
Otis Worldwide
Weyerhaeuser
Constellation Brands
Ally Financial
Murphy USA
Lear
BorgWarner
Ball
Western Digital
Tractor Supply
Williams Companies
Regeneron Pharmaceuticals
Howmet Aerospace
Owens & Minor
Jacobs Solutions
CenterPoint Energy
Packaging Corp of America
Hormel Foods
Amphenol
Live Nation Entertainment
Fidelity National Information Services
Discover Financial Services
Stanley Black & Decker
Ameren
Cigna Investment Management
Paramount Global
AES Corporation
Booking Holdings
Visa
Mastercard
WEC Energy Group
Hershey
United States Steel
ManpowerGroup
NiSource
Fluor
Universal Health Services
Kenvue
KLA Corporation
Synchrony Financial
Aon
Mohawk Industries
Expeditors International
Community Health Systems
Darden Restaurants
Campbell Soup
Eversource Energy
Biogen
American Financial Group
First American Financial
Motorola Solutions
Warner Bros. Discovery
McDonalds
Altice USA
Uber Technologies
PayPal Holdings
Encompass Health
Hartford Financial Services Group
Arista Networks
DaVita
Lumen Technologies
Jones Lang LaSalle
Autolivery
DXC Technology
LPL Financial Holdings
Laboratory Corp of America
Quest Diagnostics
Dana
CenturyLink
RPM International
Fifth Third Bancorp
Omnicom Group
Avis Budget Group
Regions Financial
AECOM
KeyCorp
Church & Dwight
Westinghouse Air Brake Technologies
Raymond James Financial
Williams-Sonoma
Wynn Resorts
MGM Resorts International
Caesars Entertainment
Huntington Bancshares
M&T Bank
Zimmer Biomet Holdings
Intuit
Autodesk
Fortive
Air Products & Chemicals
Hilton Worldwide Holdings
Marriott International
Clorox
Advance Auto Parts
PPL Corporation
Citizens Financial Group
Rockwell Automation
Aptiv
Expedia Group
Dick's Sporting Goods
Olin
Flowserve
Graphic Packaging Holding
Erie Indemnity
Fidelity National Financial
Cintas
Old Republic International
Avery Dennison
Synopsys
Fastenal
Teledyne Technologies
McCormick
Trimble
Crown Holdings
Yum! Brands
Fortinet
Dover
Ryder System
Ovintiv
Insight Enterprises
Tapestry
NetApp
ServiceNow
TransDigm Group
Smith International
W.W. Grainger
Sonoco Products
Polaris
Comerica
SBA Communications
Hubbell
Snap-on
Roper Technologies
IDEX
J.B. Hunt Transport Services
EMCOR Group
News Corp
Fox Corporation
Fair Isaac
CACI International
Lamb Weston Holdings
Foot Locker
Synovus Financial
First Solar
Builders FirstSource
Booz Allen Hamilton
Zebra Technologies
Oshkosh
Voya Financial
Cabot Oil & Gas
Wayfair
Juniper Networks
Verisign
Teradata
Mattel
Hasbro
Zoetis
Host Hotels & Resorts
Xylem
Cadence Design Systems
Verint Systems
Workday
Match Group
Realty Income
Akamai Technologies
Brunswick
Albemarle
Arch Capital Group
Leggett & Platt
XPO
Carlisle Companies
Jack Henry & Associates
Allegion
ON Semiconductor
Gartner
APA Corporation
Ralph Lauren
American Tower
PVH Corp
NVR
Taylor Morrison
PulteGroup
Toll Brothers
Smith & Wesson Brands
Flowers Foods
Watsco
Veeva Systems
Masco
Viasat
Genpact
MSC Industrial Direct
Burlington Stores
Casey's General Stores
Oscar Health
Spectrum Brands Holdings
Sealed Air
TopBuild
Science Applications International
Commvault Systems
Globe Life
ChampionX
Envision Healthcare
Smucker
Airbnb
Pool Corporation
Generac Holdings
Graco
RPC
C.H. Robinson Worldwide
Zions Bancorporation
Agilent Technologies
Coterra Energy
Analog Devices
Experian
Delek US Holdings
Selective Insurance Group
Steven Madden
Dolby Laboratories
SpartanNash
ChemTreat
MDU Resources Group
Curtiss-Wright
SPX Technologies
Resideo Technologies
Worthington Enterprises
Green Dot
Palo Alto Networks
Reliance Steel & Aluminum
WESCO International
Patterson Companies
Berry Global Group
Graybar Electric
Envista Holdings
Smith & Nephew
Robert Half
Lamar Advertising
A.O. Smith
Lincoln Electric Holdings
Mercury General
KBR
Murphy Oil
Crane Holdings
Kemper
Vail Resorts
Axon Enterprise
GXO Logistics
Sensata Technologies
Clean Harbors
Pentair
Bright Horizons Family Solutions
Brown & Brown
Genworth Financial
W.R. Grace Holdings
NorthWestern Energy Group
Prestige Consumer Healthcare
H&R Block
TripAdvisor
""";

    private const string AliasNames = """
Amazon Com
Walgreen
Walgreens Boots
Valero
Federal Home Loan Mortgage
Federal National Mortgage
Lowes
Progressive Corp
International Business Machines
TJX
Qualcomm Inc
Danaher Corp
Advanced Micro Devices
Oneok
L3Harris Tech
Pilgrims Pride
W R Berkley
Firstenergy
Expeditors Intl
Keycorp
Pultegroup
J M Smucker
W R Grace
Philip Morris
Otis
Interpublic
AmerisourceBergen
Labcorp
Raytheon
Hormel
Amphenol Corp
Discover Financial
Mattel Inc
Verint
VeriSign
Marriott
Clorox Co
Corning Inc
Comerica Inc
IDEX Corp
Rpm Intl
PVH
Masco Corp
Patterson
Graybar
CenturyTel
Hartford Financial
""";
}
