using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventStoreIssue
{
    public class DummyEventB
    {
        private readonly Guid id;

        public DummyEventB(Guid id)
        {
            this.id = id;
        }

        public Guid Id => this.id;
    }
}
