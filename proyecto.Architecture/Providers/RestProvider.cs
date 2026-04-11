using proyecto.Architecture.Helpers;

namespace proyecto.Architecture;

/// <summary>
/// Interface defining methods for RESTful operations.
/// </summary>
public interface IRestProvider
{
	/// <summary>
	/// Deletes a resource asynchronously.
	/// </summary>
	Task<string> DeleteAsync(string endpoint, string id);

	/// <summary>
	/// Retrieves a resource asynchronously.
	/// </summary>
	Task<string> GetAsync(string endpoint, string? id);

	/// <summary>
	/// Creates a resource asynchronously.
	/// </summary>
	Task<string> PostAsync(string endpoint, string content);

	/// <summary>
	/// Updates a resource asynchronously.
	/// </summary>
	Task<string> PutAsync(string endpoint, string id, string content);
}

/// <summary>
/// Implementation of the IRestProvider interface, providing methods for RESTful operations.
/// </summary>
public class RestProvider : IRestProvider
{
	public async Task<string> GetAsync(string endpoint, string? id)
	{
		try
		{
			var response = await RestProviderHelpers.CreateHttpClient(endpoint).GetAsync(id);
			return await RestProviderHelpers.GetResponse(response);
		}
		catch (Exception ex)
		{
			throw RestProviderHelpers.ThrowError(endpoint, ex);
		}
	}

	public async Task<string> PostAsync(string endpoint, string content)
	{
		try
		{
			var response = await RestProviderHelpers.CreateHttpClient(endpoint)
				.PostAsync(endpoint, RestProviderHelpers.CreateContent(content));
			return await RestProviderHelpers.GetResponse(response);
		}
		catch (Exception ex)
		{
			throw RestProviderHelpers.ThrowError(endpoint, ex);
		}
	}

	public async Task<string> PutAsync(string endpoint, string id, string content)
	{
		try
		{
			var response = await RestProviderHelpers.CreateHttpClient(endpoint)
				.PutAsync(id, RestProviderHelpers.CreateContent(content));
			return await RestProviderHelpers.GetResponse(response);
		}
		catch (Exception ex)
		{
			throw RestProviderHelpers.ThrowError(endpoint, ex);
		}
	}

	public async Task<string> DeleteAsync(string endpoint, string id)
	{
		try
		{
			var response = await RestProviderHelpers.CreateHttpClient(endpoint).DeleteAsync(id);
			return await RestProviderHelpers.GetResponse(response);
		}
		catch (Exception ex)
		{
			throw RestProviderHelpers.ThrowError(endpoint, ex);
		}
	}
}
