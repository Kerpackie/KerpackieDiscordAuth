import { useEffect, useState } from 'react'
import './App.css'

interface User {
  id: string;
  username: string;
  discriminator: string;
  avatar: string;
  global_name?: string;
}

interface Guild {
  id: string;
  name: string;
  icon: string;
  permissions: string;
}

function App() {
  const [user, setUser] = useState<User | null>(null);
  const [guilds, setGuilds] = useState<Guild[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const userRes = await fetch('/api/user');
        if (userRes.status === 401) {
          setIsAuthenticated(false);
          setLoading(false);
          return;
        }
        if (!userRes.ok) throw new Error('Failed to fetch user');
        const userData = await userRes.json();
        setUser(userData);
        setIsAuthenticated(true);

        const guildsRes = await fetch('/api/guilds');
        if (!guildsRes.ok) throw new Error('Failed to fetch guilds');
        const guildsData = await guildsRes.json();
        setGuilds(guildsData);
      } catch (err: any) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  const handleLogin = () => {
    window.location.href = `/api/login-discord?returnUrl=${encodeURIComponent(window.location.origin)}`;
  };

  const handleLogout = () => {
    window.location.href = '/api/logout';
  };

  if (loading) return <div className="loading">Loading...</div>;
  if (error) return <div className="error">Error: {error}</div>;

  return (
    <div className="container">
      <header className="header">
        <h1>Aspire Demo</h1>
        {isAuthenticated ? (
          <button onClick={handleLogout} className="btn logout">Logout</button>
        ) : (
          <button onClick={handleLogin} className="btn login">Login with Discord</button>
        )}
      </header>

      {!isAuthenticated ? (
        <div className="welcome">
          <p>Please login to view your Discord information.</p>
        </div>
      ) : (
        <div className="content">
          {user && (
            <div className="card user-card">
              <div className="user-header">
                {user.avatar && (
                  <img
                    src={`https://cdn.discordapp.com/avatars/${user.id}/${user.avatar}.${user.avatar.startsWith('a_') ? 'gif' : 'png'}`}
                    alt="Avatar"
                    className="avatar"
                  />
                )}
                <div>
                  <h2>{user.global_name || user.username}</h2>
                  <p className="username">@{user.username}</p>
                  <p className="user-id">ID: {user.id}</p>
                </div>
              </div>
            </div>
          )}

          <h2>Your Guilds</h2>
          <div className="guild-grid">
            {guilds.map(guild => (
              <div key={guild.id} className="card guild-card">
                {guild.icon ? (
                  <img
                    src={`https://cdn.discordapp.com/icons/${guild.id}/${guild.icon}.png`}
                    alt={guild.name}
                    className="guild-icon"
                  />
                ) : (
                  <div className="guild-icon-placeholder">{guild.name.substring(0, 2)}</div>
                )}
                <p className="guild-name">{guild.name}</p>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

export default App
