🧠 Business Card Recognition System (OCR)

Project Description
Business Card Recognition System is a web-based OCR application built with ASP.NET Core and C#, following Clean Architecture principles.  
It automatically extracts structured contact information from business card images and stores it in a local SQL Server database.  

Extracted data includes:
- 👤 Name  
- 🏷️ Job Title / Specialization  
- 📧 Email  
- 📞 Phone Number  
- 📍 Location / Company Information  

Architecture
Layers of the project (Clean Architecture):
- Domain: Core entities and business rules  
- Application: Use cases, DTOs, services  
- Infrastructure: OCR engine, SQL Server repository  
- Web: ASP.NET Core UI / API  

This separation ensures **scalability, maintainability, and testability**.

Features
- Upload business card images  
- OCR text extraction using **Tesseract OCR**  
- Smart parsing of contact information  
- Store extracted data in SQL Server  
- Clean Architecture layered design  
- ASP.NET Core Web Application  

Tech Stack
- Language: C#  
- Framework: ASP.NET Core  
- Architecture: Clean Architecture  
- OCR Engine: Tesseract OCR  
- Database: SQL Server LocalDB  
- Frontend: Razor Pages, HTML, CSS  
- Tools: Visual Studio, Git, GitHub  

Demo Video
📺 Demo: <video width="600" controls>
  <source src="Media/OCRDemo.mp4" type="video/mp4">
  Your browser does not support the video tag.
</video>


How to Run
1. Clone Repository:  
```bash
git clone https://github.com/norma-alduais-ai/Business-Card-OCR-Recognition-System.git
```
2. Open Business-Card-OCR-Recognition-System in Visual Studio

3. Configure Database in appsettings.json:

"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=BusinessCardDB;Trusted_Connection=True;"
}

4. Run the Application:
5. dotnet run
