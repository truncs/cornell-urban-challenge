using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.OperationalUIService.Parameters {
	public class TunableParameterFacade : MarshalByRefObject {
		private TunableParamTable table;

		public TunableParameterFacade(TunableParamTable table) {
			this.table = table;
		}

		public ICollection<TunableParam> GetParameters() {
			return table.Parameters;
		}

		public void SetParameter(TunableParam param) {
			TunableParam localParam = table.GetParam(param.Name, param.Value);
			localParam.Value = param.Value;
		}

		public TunableParam GetParameter(string param) {
			return table.GetParam(param);
		}
	}
}
