using System;
using AutoMapper;
using GestionTicketsAPI.Data;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Interfaces;

namespace GestionTicketsAPI.Services;

public class PaysService : IPaysService
{
  private readonly IPaysRepository _paysRepository;
  private readonly DataContext _context;
  private readonly IMapper _mapper;
  private readonly IWebHostEnvironment  _env;
  private readonly HttpClient           _httpClient;

  public PaysService(IPaysRepository paysRepository, IMapper mapper, IWebHostEnvironment env, DataContext context,
                       IHttpClientFactory httpFactory)
  {
    _paysRepository = paysRepository;
    _mapper = mapper;
    _env     = env;
    _context = context;
    _httpClient = httpFactory.CreateClient();
  }

  public async Task<IEnumerable<PaysDto>> GetPaysAsync()
  {
    var paysList = await _paysRepository.GetPaysAsync();
    return _mapper.Map<IEnumerable<PaysDto>>(paysList);
  }

  public async Task<IEnumerable<PaysDto>> GetPaysAsync(string searchTerm)
  {
    var paysList = await _paysRepository.GetPaysAsync(searchTerm);
    return _mapper.Map<IEnumerable<PaysDto>>(paysList);
  }

  public async Task<PaysDto?> GetPaysByIdAsync(int idPays)
  {
    var pays = await _paysRepository.GetPaysByIdAsync(idPays);
    if (pays == null) return null;
    return _mapper.Map<PaysDto>(pays);
  }

  public async Task<bool> UpdatePaysAsync(int idPays, PaysUpdateDto paysUpdateDto, IFormFile? file)
  {
    var pays = await _paysRepository.GetPaysByIdAsync(idPays);
    if (pays == null) return false;

    // Mettre à jour le nom si fourni
    if (!string.IsNullOrWhiteSpace(paysUpdateDto.Nom))
      pays.Nom = paysUpdateDto.Nom;

    // Mettre à jour le code téléphonique si fourni
    if (!string.IsNullOrWhiteSpace(paysUpdateDto.CodeTel))
      pays.CodeTel = paysUpdateDto.CodeTel;

    // Mettre à jour la photo si un fichier est fourni
    if (file != null && file.Length > 0)
    {
      // 1. Si le pays a déjà une photo en base, supprimer l'ancienne du disque
      if (pays.paysPhoto != null && !string.IsNullOrEmpty(pays.paysPhoto.Url))
      {
        // Récupération du chemin complet sur le disque
        var oldPhotoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", pays.paysPhoto.Url);
        if (System.IO.File.Exists(oldPhotoPath))
          System.IO.File.Delete(oldPhotoPath);
      }

      // 2. Sauvegarder le nouveau fichier
      var localPath = await SaveFileLocally(file);

      // 3. Mettre à jour la photo dans l'entité
      var newPhoto = new Photo
      {
        Url = localPath,       // ex: "assets/xxx.jpg"
        PublicId = null,       // plus besoin de PublicId si on ne gère plus Cloudinary
        PaysId = idPays
      };
      pays.paysPhoto = newPhoto;
    }

    return await _paysRepository.SaveAllAsync();
  }


  public async Task<PaysDto> AddPaysAsync(string nom,
                                            string? codeTel,
                                            string? fileBase64OrUrl)
    {
        if (string.IsNullOrWhiteSpace(nom))
            throw new Exception("Le nom du pays est requis.");
        if (string.IsNullOrWhiteSpace(fileBase64OrUrl))
            throw new Exception("Veuillez fournir une photo valide.");

        byte[] bytes;
        string extension;

        // 1) Si c'est une URL absolue, on la télécharge
        if (Uri.TryCreate(fileBase64OrUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == "http" || uri.Scheme == "https"))
        {
            bytes = await _httpClient.GetByteArrayAsync(uri);
            // récupère l'extension depuis le chemin d'URL
            extension = Path.GetExtension(uri.AbsolutePath).TrimStart('.');
            if (string.IsNullOrEmpty(extension))
                extension = "jpg";
        }
        else
        {
            // 2) Sinon, on suppose un DataURI Base64 comme avant
            var commaIndex = fileBase64OrUrl.IndexOf(',');
            if (commaIndex < 0)
                throw new Exception("Le format de la chaîne base64 est invalide.");

            var header     = fileBase64OrUrl.Substring(5, commaIndex - 5);
            var mimeType   = header.Split(';')[0];
            var base64Data = fileBase64OrUrl[(commaIndex + 1)..];

            try
            {
                bytes = Convert.FromBase64String(base64Data);
            }
            catch
            {
                throw new Exception("Le contenu base64 n'est pas valide.");
            }

            extension = mimeType switch
            {
                "image/png"  => "png",
                "image/gif"  => "gif",
                "image/jpeg" => "jpg",
                _            => "jpg"
            };
        }

        // 3) Sauvegarde le fichier dans wwwroot/assets
        var fileName     = $"{Guid.NewGuid()}.{extension}";
        var assetsFolder = Path.Combine(_env.WebRootPath, "assets");
        Directory.CreateDirectory(assetsFolder);
        var filePath     = Path.Combine(assetsFolder, fileName);
        await File.WriteAllBytesAsync(filePath, bytes);

        // 4) Crée l'entité et persiste
        var photo = new Photo { Url = $"assets/{fileName}" };
        var pays  = new Pays
        {
            Nom       = nom,
            CodeTel   = codeTel,
            paysPhoto = photo
        };
        _context.Pays.Add(pays);
        await _context.SaveChangesAsync();

        // 5) Retourne un DTO « plat »
        return new PaysDto
        {
            IdPays   = pays.IdPays,
            Nom      = pays.Nom,
            CodeTel  = pays.CodeTel,
            PhotoUrl = photo.Url
        };
    }
  private async Task<PaysDto> AddPaysFromFileAsync(string nom, string? codeTel, IFormFile file)
  {
    // 1. Sauvegarder le fichier localement
    var localPath = await SaveFileLocally(file);

    // 2. Créer l'entité Photo
    var photo = new Photo
    {
      Url = localPath, // ex: "assets/xxx.jpg"
      PublicId = null  // plus utilisé
    };

    // 3. Créer l'entité Pays
    var pays = new Pays
    {
      Nom = nom,
      CodeTel = codeTel,
      paysPhoto = photo
    };

    // 4. Ajouter et sauvegarder
    await _paysRepository.AddPaysAsync(pays);
    if (!await _paysRepository.SaveAllAsync())
      throw new Exception("Erreur lors de l'ajout du pays.");

    return _mapper.Map<PaysDto>(pays);
  }



  public async Task<bool> DeletePaysAsync(int idPays)
{
    // 1. Récupérer le pays en base, incluant ses sociétés
    var pays = await _paysRepository.GetPaysByIdAsync(idPays);
    if (pays == null) 
        return false;

    // 2. Refuser la suppression s’il y a au moins 1 Societe associée
    if (pays.Societes != null && pays.Societes.Any())
        throw new InvalidOperationException(
            "Impossible de supprimer ce pays : des sociétés y sont associées."
        );

    // 3. Supprimer la photo du disque
    if (!string.IsNullOrEmpty(pays.paysPhoto?.Url))
    {
        var filePath = Path.Combine(
            Directory.GetCurrentDirectory(), 
            "wwwroot", 
            pays.paysPhoto.Url
        );
        if (File.Exists(filePath)) File.Delete(filePath);
    }

    // 4. Supprimer le pays
    _paysRepository.RemovePays(pays);
    return await _paysRepository.SaveAllAsync();
}

  public async Task<bool> PaysExists(string nom)
  {
    return await _paysRepository.PaysExists(nom);
  }

  private async Task<string> SaveFileLocally(IFormFile file)
  {
    // Chemin absolu vers le dossier "assets" dans wwwroot
    // => wwwroot/assets
    var assetsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets");

    // Assurer l'existence du dossier
    if (!Directory.Exists(assetsFolder))
      Directory.CreateDirectory(assetsFolder);

    // Nom unique (ou usage direct du nom d’origine file.FileName)
    // Ici, on préfixe par un GUID pour éviter les collisions
    var uniqueFileName = Guid.NewGuid() + Path.GetExtension(file.FileName);

    // Chemin complet où sera stocké le fichier
    var filePath = Path.Combine(assetsFolder, uniqueFileName);

    // Copier le contenu du IFormFile dans un fichier physique
    using (var stream = new FileStream(filePath, FileMode.Create))
    {
      await file.CopyToAsync(stream);
    }

    // Retourne le nom de fichier ou le chemin relatif 
    // Par exemple : "assets/xxxxxxx.png"
    return Path.Combine("assets", uniqueFileName);
  }

}
