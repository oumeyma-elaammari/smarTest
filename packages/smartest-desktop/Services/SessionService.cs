using smartest_desktop.Data;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Models;
using System;

namespace smartest_desktop.Services
{
    public class SessionService
    {
        private readonly LocalDbContext _db;

        public SessionService(LocalDbContext db) => _db = db;

        public void SauvegarderSession(AuthResponse auth)
        {
            // Supprimer toute session existante
            _db.SessionsLocales.RemoveRange(_db.SessionsLocales.ToList());

            _db.SessionsLocales.Add(new SessionLocale
            {
                TokenChiffre = CryptoService.Chiffrer(auth.Token),
                Nom = auth.Nom,
                Email = auth.Email,
                Role = auth.Role,
                DateConnexion = DateTime.Now
            });

            _db.SaveChanges();
        }

        public SessionLocale? ChargerSession()
        {
            var session = _db.SessionsLocales.FirstOrDefault();
            if (session == null) return null;

            try
            {
                // Déchiffrer le token pour utilisation.
                session.TokenChiffre = CryptoService.Dechiffrer(session.TokenChiffre);
                return session;
            }
            catch
            {
                // Session locale corrompue ou créée avec une ancienne clé : on force une reconnexion.
                SupprimerSession();
                return null;
            }
        }

        public void SupprimerSession()
        {
            _db.SessionsLocales.RemoveRange(_db.SessionsLocales.ToList());
            _db.SaveChanges();
        }
    }
}