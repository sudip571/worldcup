using System;
using System.Collections.Generic;
using System.Text;
using WorldCup.Data;
using WorldCup.Models;
using Microsoft.EntityFrameworkCore;


namespace WorldCup.Services;

public class UserSessionService(AppDbContext db)
{
    public AppUser? CurrentUser { get; private set; }
    public bool IsLoggedIn => CurrentUser is not null;

    public async Task<bool> LoginOrRegisterAsync(string username, string displayName)
    {
        username = username.Trim().ToLower();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user is null)
        {
            user = new AppUser { Username = username, DisplayName = displayName };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        CurrentUser = user;
        return true;
    }

    public async Task UpdateProfileAsync(string displayName, string avatarUrl)
    {
        if (CurrentUser is null) return;
        CurrentUser.DisplayName = displayName;
        CurrentUser.AvatarUrl = avatarUrl;
        db.Users.Update(CurrentUser);
        await db.SaveChangesAsync();
    }

    public void Logout() => CurrentUser = null;
}