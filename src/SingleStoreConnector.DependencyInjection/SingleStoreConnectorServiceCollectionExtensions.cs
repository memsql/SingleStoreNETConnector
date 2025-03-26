using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace SingleStoreConnector;

/// <summary>
/// Extension method for setting up SingleStoreConnector services in an <see cref="IServiceCollection" />.
/// </summary>
public static class SingleStoreConnectorServiceCollectionExtensions
{
	/// <summary>
	/// Registers a <see cref="SingleStoreDataSource" /> and a <see cref="SingleStoreConnection" /> in the <see cref="IServiceCollection" />.
	/// </summary>
	/// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
	/// <param name="connectionString">A SingleStore connection string.</param>
	/// <param name="connectionLifetime">The lifetime with which to register the <see cref="SingleStoreConnection" /> in the container. Defaults to <see cref="ServiceLifetime.Transient" />.</param>
	/// <param name="dataSourceLifetime">The lifetime with which to register the <see cref="SingleStoreDataSource" /> service in the container. Defaults to <see cref="ServiceLifetime.Singleton" />.</param>
	/// <returns>The same service collection so that multiple calls can be chained.</returns>
	public static IServiceCollection AddSingleStoreDataSource(
		this IServiceCollection serviceCollection,
		string connectionString,
		ServiceLifetime connectionLifetime = ServiceLifetime.Transient,
		ServiceLifetime dataSourceLifetime = ServiceLifetime.Singleton) =>
		DoAddSingleStoreDataSource(serviceCollection, connectionString, dataSourceBuilderAction: null, connectionLifetime, dataSourceLifetime);

	/// <summary>
	/// Registers a <see cref="SingleStoreDataSource" /> and a <see cref="SingleStoreConnection" /> in the <see cref="IServiceCollection" />.
	/// </summary>
	/// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
	/// <param name="connectionString">A SingleStore connection string.</param>
	/// <param name="dataSourceBuilderAction">An action to configure the <see cref="SingleStoreDataSourceBuilder" /> for further customizations of the <see cref="SingleStoreDataSource" />.</param>
	/// <param name="connectionLifetime">The lifetime with which to register the <see cref="SingleStoreConnection" /> in the container. Defaults to <see cref="ServiceLifetime.Transient" />.</param>
	/// <param name="dataSourceLifetime">The lifetime with which to register the <see cref="SingleStoreDataSource" /> service in the container. Defaults to <see cref="ServiceLifetime.Singleton" />.</param>
	/// <returns>The same service collection so that multiple calls can be chained.</returns>
	public static IServiceCollection AddSingleStoreDataSource(
		this IServiceCollection serviceCollection,
		string connectionString,
		Action<SingleStoreDataSourceBuilder> dataSourceBuilderAction,
		ServiceLifetime connectionLifetime = ServiceLifetime.Transient,
		ServiceLifetime dataSourceLifetime = ServiceLifetime.Singleton) =>
		DoAddSingleStoreDataSource(serviceCollection, connectionString, dataSourceBuilderAction, connectionLifetime, dataSourceLifetime);

	private static IServiceCollection DoAddSingleStoreDataSource(
		this IServiceCollection serviceCollection,
		string connectionString,
		Action<SingleStoreDataSourceBuilder>? dataSourceBuilderAction,
		ServiceLifetime connectionLifetime,
		ServiceLifetime dataSourceLifetime)
	{
		serviceCollection.TryAdd(
			new ServiceDescriptor(
				typeof(SingleStoreDataSource),
				x =>
				{
					var dataSourceBuilder = new SingleStoreDataSourceBuilder(connectionString)
						.UseLoggerFactory(x.GetService<ILoggerFactory>());
					dataSourceBuilderAction?.Invoke(dataSourceBuilder);
					return dataSourceBuilder.Build();
				},
				dataSourceLifetime));

		serviceCollection.TryAdd(new ServiceDescriptor(typeof(SingleStoreConnection), x => x.GetRequiredService<SingleStoreDataSource>().CreateConnection(), connectionLifetime));

#if NET7_0_OR_GREATER
		serviceCollection.TryAdd(new ServiceDescriptor(typeof(DbDataSource), x => x.GetRequiredService<SingleStoreDataSource>(), dataSourceLifetime));
#endif

		serviceCollection.TryAdd(new ServiceDescriptor(typeof(DbConnection), x => x.GetRequiredService<SingleStoreConnection>(), connectionLifetime));

		return serviceCollection;
	}
}
