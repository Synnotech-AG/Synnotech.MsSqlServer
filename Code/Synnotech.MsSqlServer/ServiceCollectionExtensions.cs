using System;
using Light.GuardClauses;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Synnotech.DatabaseAbstractions;

namespace Synnotech.MsSqlServer
{
    /// <summary>
    /// Provides extensions for registering a SQL connection with the DI container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers an <see cref="ISessionFactory{TSessionAbstraction}" /> for the specified session. You can inject this session factory
        /// into client code to resolve your session asynchronously. When resolved, a new session is created and initialized asynchronously
        /// when the session implements <see cref="IInitializeAsync" />.
        /// <code>
        /// public class MySessionClient
        /// {
        ///     public MySessionClient(ISessionFactory&lt;IMySession> sessionFactory) =>
        ///         SessionFactory = sessionFactory;
        /// 
        ///     private ISessionFactory&lt;IMySession> SessionFactory { get; }
        /// 
        ///     public async Task SomeMethod()
        ///     {
        ///         await using var session = await SessionFactory.OpenSessionAsync();
        ///         // do something useful with your session
        ///     }
        /// }
        /// </code>
        /// </summary>
        /// <typeparam name="TAbstractSession">The interface that your session implements. It must implement <see cref="IAsyncSession" />.</typeparam>
        /// <typeparam name="TConcreteSession">The Linq2Db session implementation that performs the actual database I/O.</typeparam>
        /// <param name="services">The collection that holds all registrations for the DI container.</param>
        /// <param name="sessionLifetime">
        /// The lifetime of the session (optional). Should be either <see cref="ServiceLifetime.Transient" /> or
        /// <see cref="ServiceLifetime.Scoped" />. The default is <see cref="ServiceLifetime.Transient" />.
        /// </param>
        /// <param name="factoryLifetime">The lifetime for the session factory. It's usually ok for them to be a singleton.</param>
        /// <param name="registerCreateSessionDelegate">
        /// The value indicating whether a Func&lt;TAbstraction> is also registered with the DI container (optional).
        /// This factory delegate is necessary for the <see cref="SessionFactory{T}" /> to work properly. The default value is true.
        /// You can set this value to false if you use a proper DI container like LightInject that offers function factories. https://www.lightinject.net/#function-factories
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services" /> is null.</exception>
        public static IServiceCollection AddSessionFactoryFor<TAbstractSession, TConcreteSession>(this IServiceCollection services,
                                                                                                  ServiceLifetime sessionLifetime = ServiceLifetime.Transient,
                                                                                                  ServiceLifetime factoryLifetime = ServiceLifetime.Singleton,
                                                                                                  bool registerCreateSessionDelegate = true)
            where TAbstractSession : class, IAsyncReadOnlySession
            where TConcreteSession : class, TAbstractSession
        {
            services.MustNotBeNull(nameof(services));

            services.Add(new ServiceDescriptor(typeof(TAbstractSession), typeof(TConcreteSession), sessionLifetime));
            services.Add(new ServiceDescriptor(typeof(ISessionFactory<TAbstractSession>), typeof(SessionFactory<TAbstractSession>), factoryLifetime));
            if (registerCreateSessionDelegate)
                services.AddSingleton<Func<TAbstractSession>>(c => c.GetRequiredService<TAbstractSession>);
            return services;
        }

        /// <summary>
        /// Registers a <see cref="SqlConnection" /> with the DI container.
        /// </summary>
        /// <param name="services">The collection that holds all registrations for the DI container.</param>
        /// <param name="connectionString">The connection string that will be passed as to the <see cref="SqlConnection" /> constructor.</param>
        /// <param name="sqlConnectionLifetime">
        /// The lifetime that is used for the SqlConnection. The default value is <see cref="ServiceLifetime.Transient" />.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services" /> is null.</exception>
        public static IServiceCollection AddSqlConnection(this IServiceCollection services,
                                                          string connectionString,
                                                          ServiceLifetime sqlConnectionLifetime = ServiceLifetime.Transient)
        {
            services.MustNotBeNull(nameof(services));

            services.Add(new ServiceDescriptor(typeof(SqlConnection), _ => new SqlConnection(connectionString), sqlConnectionLifetime));
            return services;
        }
    }
}