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
		DoAddSingleStoreDataSource(serviceCollection, connectionString, dataSourceBuilderAction: null, connectionLifetime, dataSourceLifetime, builderActionState: null);

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
				DoAddSingleStoreDataSource(serviceCollection, connectionString, DataSourceBuilderThunk, connectionLifetime, dataSourceLifetime, builderActionState: dataSourceBuilderAction);

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
		Action<IServiceProvider, SingleStoreDataSourceBuilder> dataSourceBuilderAction,
		ServiceLifetime connectionLifetime = ServiceLifetime.Transient,
		ServiceLifetime dataSourceLifetime = ServiceLifetime.Singleton) =>
		DoAddSingleStoreDataSource(serviceCollection, connectionString, ServiceProviderDataSourceBuilderThunk, connectionLifetime, dataSourceLifetime, builderActionState: dataSourceBuilderAction);

	/// <summary>
	/// Registers a <see cref="SingleStoreDataSource" /> and a <see cref="SingleStoreConnection" /> in the <see cref="IServiceCollection" />.
	/// </summary>
	/// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
	/// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
	/// <param name="connectionString">A SingleStore connection string.</param>
	/// <param name="connectionLifetime">The lifetime with which to register the <see cref="SingleStoreConnection" /> in the container. Defaults to <see cref="ServiceLifetime.Transient" />.</param>
	/// <param name="dataSourceLifetime">The lifetime with which to register the <see cref="SingleStoreDataSource" /> service in the container. Defaults to <see cref="ServiceLifetime.Singleton" />.</param>
	/// <returns>The same service collection so that multiple calls can be chained.</returns>
	/// <remarks>If the <paramref name="serviceKey"/> is a <see langword="string"/>, it will automatically be used to initialize the data source name.</remarks>
	public static IServiceCollection AddKeyedSingleStoreDataSource(
		this IServiceCollection serviceCollection,
		object? serviceKey,
		string connectionString,
		ServiceLifetime connectionLifetime = ServiceLifetime.Transient,
		ServiceLifetime dataSourceLifetime = ServiceLifetime.Singleton) =>
		DoAddSingleStoreDataSource(serviceCollection, serviceKey, connectionString, dataSourceBuilderAction: null, connectionLifetime, dataSourceLifetime, builderActionState: null);

	/// <summary>
	/// Registers a <see cref="SingleStoreDataSource" /> and a <see cref="SingleStoreConnection" /> in the <see cref="IServiceCollection" />.
	/// </summary>
	/// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
	/// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
	/// <param name="connectionString">A SingleStore connection string.</param>
	/// <param name="dataSourceBuilderAction">An action to configure the <see cref="SingleStoreDataSourceBuilder" /> for further customizations of the <see cref="SingleStoreDataSource" />.</param>
	/// <param name="connectionLifetime">The lifetime with which to register the <see cref="SingleStoreConnection" /> in the container. Defaults to <see cref="ServiceLifetime.Transient" />.</param>
	/// <param name="dataSourceLifetime">The lifetime with which to register the <see cref="SingleStoreDataSource" /> service in the container. Defaults to <see cref="ServiceLifetime.Singleton" />.</param>
	/// <returns>The same service collection so that multiple calls can be chained.</returns>
	/// <remarks>If the <paramref name="serviceKey"/> is a <see langword="string"/>, it will automatically be used to initialize the data source name; this can
	/// be overridden by the <paramref name="dataSourceBuilderAction"/> configuration action.</remarks>
	public static IServiceCollection AddKeyedSingleStoreDataSource(
		this IServiceCollection serviceCollection,
		object? serviceKey,
		string connectionString,
		Action<SingleStoreDataSourceBuilder> dataSourceBuilderAction,
		ServiceLifetime connectionLifetime = ServiceLifetime.Transient,
		ServiceLifetime dataSourceLifetime = ServiceLifetime.Singleton) =>
		DoAddSingleStoreDataSource(serviceCollection, serviceKey, connectionString, DataSourceBuilderThunk, connectionLifetime, dataSourceLifetime, builderActionState: dataSourceBuilderAction);

	/// <summary>
	/// Registers a <see cref="SingleStoreDataSource" /> and a <see cref="SingleStoreConnection" /> in the <see cref="IServiceCollection" />.
	/// </summary>
	/// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
	/// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
	/// <param name="connectionString">A SingleStore connection string.</param>
	/// <param name="dataSourceBuilderAction">An action to configure the <see cref="SingleStoreDataSourceBuilder" /> for further customizations of the <see cref="SingleStoreDataSource" />.</param>
	/// <param name="connectionLifetime">The lifetime with which to register the <see cref="SingleStoreConnection" /> in the container. Defaults to <see cref="ServiceLifetime.Transient" />.</param>
	/// <param name="dataSourceLifetime">The lifetime with which to register the <see cref="SingleStoreDataSource" /> service in the container. Defaults to <see cref="ServiceLifetime.Singleton" />.</param>
	/// <returns>The same service collection so that multiple calls can be chained.</returns>
	/// <remarks>If the <paramref name="serviceKey"/> is a <see langword="string"/>, it will automatically be used to initialize the data source name; this can
	/// be overridden by the <paramref name="dataSourceBuilderAction"/> configuration action.</remarks>
	public static IServiceCollection AddKeyedSingleStoreDataSource(
		this IServiceCollection serviceCollection,
		object? serviceKey,
		string connectionString,
		Action<IServiceProvider, SingleStoreDataSourceBuilder> dataSourceBuilderAction,
		ServiceLifetime connectionLifetime = ServiceLifetime.Transient,
		ServiceLifetime dataSourceLifetime = ServiceLifetime.Singleton) =>
		DoAddSingleStoreDataSource(serviceCollection, serviceKey, connectionString, ServiceProviderDataSourceBuilderThunk, connectionLifetime, dataSourceLifetime, builderActionState: dataSourceBuilderAction);

	private static IServiceCollection DoAddSingleStoreDataSource(
		this IServiceCollection serviceCollection,
		string connectionString,
		Action<IServiceProvider, SingleStoreDataSourceBuilder, object?>? dataSourceBuilderAction,
		ServiceLifetime connectionLifetime,
		ServiceLifetime dataSourceLifetime,
		object? builderActionState)
	{
		serviceCollection.TryAdd(
			new ServiceDescriptor(
				typeof(SingleStoreDataSource),
				serviceProvider =>
				{
					var dataSourceBuilder = new SingleStoreDataSourceBuilder(connectionString)
						.UseLoggerFactory(serviceProvider.GetService<ILoggerFactory>());
					dataSourceBuilderAction?.Invoke(serviceProvider, dataSourceBuilder, builderActionState);
					return dataSourceBuilder.Build();
				},
				dataSourceLifetime));

		serviceCollection.TryAdd(new ServiceDescriptor(typeof(SingleStoreConnection), static x => x.GetRequiredService<SingleStoreDataSource>().CreateConnection(), connectionLifetime));

#if NET7_0_OR_GREATER
		serviceCollection.TryAdd(new ServiceDescriptor(typeof(DbDataSource), static x => x.GetRequiredService<SingleStoreDataSource>(), dataSourceLifetime));
#endif

		serviceCollection.TryAdd(new ServiceDescriptor(typeof(DbConnection), static x => x.GetRequiredService<SingleStoreConnection>(), connectionLifetime));

		return serviceCollection;
	}

	private static IServiceCollection DoAddSingleStoreDataSource(
		this IServiceCollection serviceCollection,
		object? serviceKey,
		string connectionString,
		Action<IServiceProvider, SingleStoreDataSourceBuilder, object?>? dataSourceBuilderAction,
		ServiceLifetime connectionLifetime,
		ServiceLifetime dataSourceLifetime,
		object? builderActionState)
	{
		serviceCollection.TryAdd(
			new ServiceDescriptor(
				typeof(SingleStoreDataSource),
				serviceKey,
				(serviceProvider, serviceKey) =>
				{
					var dataSourceBuilder = new SingleStoreDataSourceBuilder(connectionString)
						.UseLoggerFactory(serviceProvider.GetService<ILoggerFactory>())
						.UseName(serviceKey as string);
					dataSourceBuilderAction?.Invoke(serviceProvider, dataSourceBuilder, builderActionState);
					return dataSourceBuilder.Build();
				},
				dataSourceLifetime));

		serviceCollection.TryAdd(new ServiceDescriptor(typeof(SingleStoreConnection), serviceKey, static (sp, sk) => sp.GetRequiredKeyedService<SingleStoreDataSource>(sk).CreateConnection(), connectionLifetime));

#if NET7_0_OR_GREATER
		serviceCollection.TryAdd(new ServiceDescriptor(typeof(DbDataSource), serviceKey, static (sp, sk) => sp.GetRequiredKeyedService<SingleStoreDataSource>(sk), dataSourceLifetime));
#endif

		serviceCollection.TryAdd(new ServiceDescriptor(typeof(DbConnection), serviceKey, static (sp, sk) => sp.GetRequiredKeyedService<SingleStoreConnection>(sk), connectionLifetime));

		return serviceCollection;
	}

	private static void DataSourceBuilderThunk(IServiceProvider serviceProvider, SingleStoreDataSourceBuilder dataSourceBuilder, object? state) =>
		((Action<SingleStoreDataSourceBuilder>) state!)(dataSourceBuilder);

	private static void ServiceProviderDataSourceBuilderThunk(IServiceProvider serviceProvider, SingleStoreDataSourceBuilder dataSourceBuilder, object? state) =>
		((Action<IServiceProvider, SingleStoreDataSourceBuilder>) state!)(serviceProvider, dataSourceBuilder);
}
