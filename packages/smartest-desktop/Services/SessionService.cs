using Microsoft.EntityFrameworkCore;
using smartest_desktop.Data;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Models;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace smartest_desktop.Services
{
    public class SessionService
    {
        private readonly LocalDbContext _db;

        public SessionService(LocalDbContext db) => _db = db;

        public void SauvegarderSession(AuthResponse auth)
        {
            ArgumentNullException.ThrowIfNull(auth);
            ArgumentNullException.ThrowIfNull(auth.Token);

            try
            {
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
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException(
                    "Impossible de sécuriser le jeton avant enregistrement local.", ex);
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException(
                    "Enregistrement de session local impossible.", ex);
            }
        }

        public SessionLocale? ChargerSession()
        {
            try
            {
                var session = _db.SessionsLocales.FirstOrDefault();
                if (session == null) return null;

                try
                {
                    session.TokenChiffre = CryptoService.Dechiffrer(session.TokenChiffre);
                    return session;
                }
                catch (Exception)
                {
                    SupprimerSession();
                    return null;
                }
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException(
                    "Lecture de session locale impossible.", ex);
            }
        }

        public void SupprimerSession()
        {
            try
            {
                _db.SessionsLocales.RemoveRange(_db.SessionsLocales.ToList());
                _db.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException(
                    "Suppression de session locale impossible.", ex);
            }
        }
    }
}
